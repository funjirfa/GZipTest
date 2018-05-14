using System;
using System.Diagnostics;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        private static int _cores = Environment.ProcessorCount * 2;

        private static BlockPool _originalBlocks = new BlockPool(_cores);

        private static BlockPool _modifiedBlocks = new BlockPool(_cores);

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

            if (options.Command == Operation.Compress)
            {
                Command = new Compressor();
            }
            else
            {
                Command = new Decompressor();
            }

            ProgressReport progress = new ProgressReport();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Command.ShowProgress += progress.ShowProgress;

            _writer = new Thread( delegate() { Command.Writer( options.Destination, ref _modifiedBlocks ); } );
            _reader = new Thread( delegate() { Command.Reader( options.Source, ref _originalBlocks ); } );

            for (int i = 0; i < _cores; i++)
            {
                _handlers[i] = new Thread(delegate () { Command.Handler( ref _originalBlocks, ref _modifiedBlocks ); } );
            }

            _writer.Start();

            foreach (Thread handler in _handlers)
            {
                handler.Start();
            }

            _reader.Start();
            _reader.Join();

            foreach (Thread handler in _handlers)
            {
                handler.Join();
            }

            _writer.Join();

            Command.ShowProgress -= progress.ShowProgress;

            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            Console.WriteLine("\nDONE!\t({0:D2}:{1:D2}:{2:D2})", ts.Hours, ts.Minutes, ts.Seconds);
        }
    }
}
