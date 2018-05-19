using System;
using System.IO;

namespace GZipTest
{
    public class ParseArgs
    {
        private const long FAT32_MAX_FILE_SIZE = 4294967295;

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

            if (Command == Operation.Compress)
            {
                FileInfo fi = new FileInfo(Source);
                FileSettings.Length = fi.Length;
                FileSettings.BlockCount = fi.Length / GZip.BUFFER_SIZE;
                if (fi.Length % GZip.BUFFER_SIZE > 0)
                {
                    FileSettings.BlockCount++;
                }
            }
            else
            {
                using (BinaryReader br = new BinaryReader(new FileStream(Source, FileMode.Open, FileAccess.Read, FileShare.None)))
                {
                    FileSettings.Length = br.ReadInt64();
                    FileSettings.BlockCount = br.ReadInt64();
                }

                FileInfo fi = new FileInfo(Destination);
                DriveInfo drive = new DriveInfo(fi.Directory.Root.FullName);

                if (drive.DriveFormat == "FAT32" && FileSettings.Length > FAT32_MAX_FILE_SIZE)
                {
                    throw new IOException("ERROR: недостаточно места на диске записи распакованного файла (ограничение FAT32)");
                }
            }

            if (FileSettings.Length % GZip.BUFFER_SIZE > 0)
            {
                FileSettings.LastBlockLength = (int)(FileSettings.Length % GZip.BUFFER_SIZE);
            }
        }
    }
}
