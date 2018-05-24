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

            switch (args[0])
            {
                case "compress":
                    Command = Operation.Compress;
                    break;
                case "decompress":
                    Command = Operation.Decompress;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("args[0]", "'compress | decompress'", "ERROR: недопустимая команда");
            }

            if (!File.Exists(args[1]))
            {
                throw new FileNotFoundException("ERROR: указан неверный путь к файлу", args[1]);
            }

            Source = args[1];

            if (!Directory.Exists(Path.GetDirectoryName(args[2])))
            {
                throw new DirectoryNotFoundException(string.Format("ERROR: указанного пути не существует ({0})", Path.GetDirectoryName(args[2])));
            }

            Destination = args[2];
        }
    }
}
