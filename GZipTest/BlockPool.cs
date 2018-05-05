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
        
        public void Enqueue(KeyValuePair<int, byte[]> block)
        {
            if (block.Value == null)
            {
                throw new ArgumentNullException("block.Value", "ERROR: блок байтов пуст");
            }

            lock (_mutex)
            {
                if (_isCompleted)
                {
                    throw new InvalidOperationException("ERROR: общий пул блоков уже недоступен");
                }

                _queue.Enqueue(block);

                Monitor.Pulse(_mutex);
            }
        }

        public KeyValuePair<int, byte[]> Dequeue()
        {
            lock (_mutex)
            {
                while (_queue.Count == 0 && !_isCompleted)
                {
                    Monitor.Wait(_mutex);
                }

                if (_queue.Count == 0)
                {
                    return new KeyValuePair<int, byte[]>( -1, null );
                }

                return _queue.Dequeue();
            }
        }

        public void Complete()
        {
            lock (_mutex)
            {
                _isCompleted = true;

                Monitor.PulseAll(_mutex);
            }
        }

        public void Progress(string command, double progress)
        {
            lock (_mutex)
            {
                Console.Write("{0}:\t{1:P}\r", command, progress);

                Monitor.Pulse(_mutex);
            }
        }
    }
}
