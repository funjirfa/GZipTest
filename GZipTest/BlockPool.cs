using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    public class BlockPool
    {
        private object _mutex = new object();

        private Queue<KeyValuePair<int, byte[]>> _queue = new Queue<KeyValuePair<int, byte[]>>();

        private bool _isCompleted = false;

        private readonly int BlockCount;

        private bool _isReady = true;

        public BlockPool(int blockCount)
        {
            BlockCount = blockCount;
        }

        public void Enqueue(KeyValuePair<int, byte[]> block)
        {
            if (block.Value == null)
            {
                throw new ArgumentNullException("block.Value", "ERROR: блок байтов пуст");
            }

            lock (_mutex)
            {
                while (!_isReady)
                {
                    Monitor.Wait(_mutex);
                }

                if (_isCompleted)
                {
                    throw new InvalidOperationException("ERROR: общий пул блоков уже недоступен");
                }

                _queue.Enqueue(block);

                if (_queue.Count == BlockCount)
                {
                    _isReady = false;
                    Monitor.PulseAll(_mutex);
                }

                Monitor.Pulse(_mutex);
            }
        }

        public KeyValuePair<int, byte[]> Dequeue()
        {
            lock (_mutex)
            {
                while (_queue.Count == 0 && !_isCompleted)
                {
                    Monitor.Wait(_mutex, 1000);
                }

                while (_isReady)
                {
                    Monitor.Wait(_mutex, 1000);
                }

                if (_queue.Count == 0)
                {
                    return new KeyValuePair<int, byte[]>(-1, null);
                }

                KeyValuePair<int, byte[]> block = _queue.Dequeue();

                if (_queue.Count == 0)
                {
                    _isReady = true;
                    Monitor.PulseAll(_mutex);
                }

                return block;
            }
        }

        public void Complete()
        {
            lock (_mutex)
            {
                _isCompleted = true;

                _isReady = false;

                Monitor.PulseAll(_mutex);
            }
        }
    }
}
