using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    public class TaskPool
    {
        private Queue<KeyValuePair<int, byte[]>> _taskPool;

        private readonly int Capacity;

        private int _setCounter = 0;

        private int _getCounter = 0;

        private bool _isCancel = false;

        public TaskPool(int capacity)
        {
            _taskPool = new Queue<KeyValuePair<int, byte[]>>(capacity);
            Capacity = capacity;
        }

        public bool TrySet(int blockNumber, byte[] blockValue)
        {
            if (blockValue == null)
            {
                _isCancel = true;
                Monitor.PulseAll(_taskPool);
                throw new ArgumentNullException("blockValue", "ERROR: некорректное значение блока");
            }

            lock (_taskPool)
            {
                while (_taskPool.Count >= Capacity)
                {
                    if (_isCancel)
                    {
                        Monitor.PulseAll(_taskPool);
                        return false;
                    }

                    if (_setCounter == FileSettings.BlockCount)
                    {
                        _isCancel = true;
                        Monitor.PulseAll(_taskPool);
                        throw new IndexOutOfRangeException("ERROR: превышено допустимое количество блоков");
                    }

                    Monitor.Pulse(_taskPool);
                    Monitor.Wait(_taskPool);
                }

                if (_isCancel)
                {
                    return false;
                }

                _taskPool.Enqueue(new KeyValuePair<int, byte[]>(blockNumber, blockValue));
                _setCounter++;

                Monitor.Pulse(_taskPool);
                return true;
            }
        }

        public bool TryGet(out int blockNumber, out byte[] blockValue)
        {
            lock (_taskPool)
            {
                while (_taskPool.Count == 0)
                {
                    if (_isCancel || _getCounter == FileSettings.BlockCount)
                    {
                        blockNumber = -1;
                        blockValue = null;

                        Monitor.PulseAll(_taskPool);
                        return false;
                    }

                    Monitor.Pulse(_taskPool);
                    Monitor.Wait(_taskPool);
                }

                if (_isCancel)
                {
                    blockNumber = -1;
                    blockValue = null;

                    Monitor.PulseAll(_taskPool);
                    return false;
                }

                KeyValuePair<int, byte[]> block = _taskPool.Dequeue();
                _getCounter++;

                blockNumber = block.Key;
                blockValue = block.Value;

                Monitor.Pulse(_taskPool);
                return true;
            }
        }

        public void Cancel()
        {
            lock (_taskPool)
            {
                _isCancel = true;

                Monitor.PulseAll(_taskPool);
            }
        }
    }
}
