using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    public class Decompressor : ICommand
    {
        private const long FAT32_MAX_FILE_SIZE = 4294967295;

        private long _originLength;

        private long _blockCount;

        private bool _isDelete = false;

        public event TerminationEventHandler Terminate;

        public event ProgressEventHandler ShowProgress;

        public void Reader(string source, ref TaskPool readerTaskPool)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
                {
                    _originLength = br.ReadInt64();
                    _blockCount = br.ReadInt64();

                    for (int count = 0; count < _blockCount; count++)
                    {
                        int blockNumber = br.ReadInt32();
                        int blockLength = br.ReadInt32();
                        byte[] blockValue = br.ReadBytes(blockLength);

                        if (blockValue == null)
                        {
                            throw new ArgumentNullException("blockValue", "ERROR: некорректное значение блока");
                        }

                        if (!readerTaskPool.TrySet(blockNumber, blockValue))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _isDelete = true;
                Terminate();
                Console.WriteLine(e.Message);
                return;
            }
        }

        public void Handler(ref TaskPool readerTaskPool, ref TaskPool writerTaskPool)
        {
            int blockNumber = -1;
            byte[] blockValue = null;

            while (true)
            {
                try
                {
                    if (!readerTaskPool.TryGet(out blockNumber, out blockValue))
                    {
                        return;
                    }

                    if (blockValue == null)
                    {
                        break;
                    }

                    byte[] decompressedBlock = GZip.Decompress(blockValue);

                    if (!writerTaskPool.TrySet(blockNumber, decompressedBlock))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _isDelete = true;
                    Terminate();
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }

        public void Writer(string destination, ref TaskPool writerTaskPool)
        {
            try
            {
                FileInfo fi = new FileInfo(destination);
                DriveInfo drive = new DriveInfo(fi.Directory.Root.FullName);
                if (drive.DriveFormat == "FAT32" && _originLength > FAT32_MAX_FILE_SIZE)
                {
                    throw new IOException("ERROR: недостаточно места на диске записи распакованного файла (ограничение FAT32)");
                }
            }
            catch (Exception e)
            {
                Terminate();
                Console.WriteLine(e.Message);
                return;
            }

            int counter = 0;

            int blockNumber = -1;
            byte[] blockValue = null;

            Dictionary<int, byte[]> buffer = new Dictionary<int, byte[]>();

            try
            {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    while (true)
                    {
                        if (!writerTaskPool.TryGet(out blockNumber, out blockValue))
                        {
                            return;
                        }

                        if (blockValue == null)
                        {
                            break;
                        }

                        buffer[blockNumber] = blockValue;

                        while (buffer.ContainsKey(counter))
                        {
                            bw.Write(buffer[counter]);
                            buffer.Remove(counter);

                            counter++;
                            ShowProgress((double)counter / _blockCount);

                            if (counter == _blockCount)
                            {
                                Terminate();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _isDelete = true;
                Terminate();
                Console.WriteLine(e.Message);
            }

            if (_isDelete)
            {
                try
                {
                    File.Delete(destination);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }
    }
}
