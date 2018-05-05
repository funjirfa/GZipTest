using System;
using System.IO;

namespace GZipTest
{
    public class ParseArgs
    {
        public Operation Command;

        public readonly string Source;

        public readonly string Destination;

        public ParseArgs(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("ERROR: допустимое количество параметров - 3", "args");
            }

            if (args[0] != "compress" && args[0] != "decompress")
            {
                throw new ArgumentOutOfRangeException("args[1]", "'compress | decompress'", "ERROR: недопустимая команда");
            }

            if (args[0] == "compress")
            {
                Command = Operation.Compress;
            }
            else
            {
                Command = Operation.Decompress; 
            }

            Source = args[1];

            if (!File.Exists(Source))
            {
                throw new FileNotFoundException("ERROR: указан неверный путь к файлу", Source);
            }

            Destination = args[2];

            if (!Directory.Exists(Path.GetDirectoryName(Destination)))
            {
                throw new DirectoryNotFoundException(string.Format("ERROR: указанного пути не существует ({0})", Path.GetDirectoryName(Destination)));
            }
        }
    }
}
