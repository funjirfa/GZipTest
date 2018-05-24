using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    public class TaskPool
    {
        private Queue<KeyValuePair<int, byte[]>> _taskPool;

        private readonly int Capacity;

        private bool _isTerminate = false;

        public TaskPool(int capacity)
        {
            _taskPool = new Queue<KeyValuePair<int, byte[]>>(capacity);
            Capacity = capacity;
        }

        public bool TrySet(int blockNumber, byte[] blockValue)
        {
            lock (_taskPool)
            {
                while (_taskPool.Count >= Capacity)
                {
                    if (_isTerminate)
                    {
                        return false;
                    }

                    Monitor.Wait(_taskPool);
                }

                if (_isTerminate)
                {
                    return false;
                }

                _taskPool.Enqueue(new KeyValuePair<int, byte[]>(blockNumber, blockValue));

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
                    if (_isTerminate)
                    {
                        blockNumber = -1;
                        blockValue = null;

                        return false;
                    }

                    Monitor.Wait(_taskPool);
                }

                if (_isTerminate)
                {
                    blockNumber = -1;
                    blockValue = null;

                    return false;
                }

                KeyValuePair<int, byte[]> block = _taskPool.Dequeue();

                blockNumber = block.Key;
                blockValue = block.Value;

                Monitor.Pulse(_taskPool);
                return true;
            }
        }

        public void Terminate()
        {
            lock (_taskPool)
            {
                _isTerminate = true;

                Monitor.PulseAll(_taskPool);
            }
        }
    }
}
