using System;
using System.Diagnostics;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        private static int _cores = Environment.ProcessorCount * 2;

        private static TaskPool _readerTaskPool;

        private static TaskPool _writerTaskPool;

        private static Thread _reader;

        private static Thread[] _handlers = new Thread[_cores];

        private static Thread _writer;

        static void Main(string[] args)
        {
            ParseArgs options;

            try
            {
                options = new ParseArgs(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            ICommand Command = null;

            ProgressReport progress;

            if (options.Command == Operation.Compress)
            {
                Command = new Compressor();
                progress = new ProgressReport("Compress");
            }
            else
            {
                Command = new Decompressor();
                progress = new ProgressReport("Decompress");
            }

            _readerTaskPool = new TaskPool(_cores);
            _writerTaskPool = new TaskPool(_cores);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Command.ShowProgress += progress.ShowProgress;
            Command.Terminate += _readerTaskPool.Terminate;
            Command.Terminate += _writerTaskPool.Terminate;

            _reader = new Thread(delegate () { Command.Reader(options.Source, ref _readerTaskPool); });

            for (int i = 0; i < _cores; i++)
            {
                _handlers[i] = new Thread(delegate () { Command.Handler(ref _readerTaskPool, ref _writerTaskPool); });
            }

            _writer = new Thread(delegate () { Command.Writer(options.Destination, ref _writerTaskPool); });

            _reader.Start();
            foreach (Thread handler in _handlers)
            {
                handler.Start();
            }
            _writer.Start();

            _writer.Join();
            foreach (Thread handler in _handlers)
            {
                handler.Join();
            }
            _reader.Join();

            sw.Stop();
            progress.Done(sw.Elapsed);

            Command.Terminate -= _writerTaskPool.Terminate;
            Command.Terminate -= _readerTaskPool.Terminate;
            Command.ShowProgress -= progress.ShowProgress;
        }
    }
}
