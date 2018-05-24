using System;
using System.IO;

namespace GZipTest
{
    public class Compressor : ICommand
    {
        private long _sourceLength;

        private long _blockCount;

        private bool _isDelete = false;

        public event TerminationEventHandler Terminate;

        public event ProgressEventHandler ShowProgress;

        public void Reader(string source, ref TaskPool readerTaskPool)
        {
            try
            {
                FileInfo fi = new FileInfo(source);
                _sourceLength = fi.Length;
                _blockCount = fi.Length / GZip.BUFFER_SIZE;
                if (fi.Length % GZip.BUFFER_SIZE > 0)
                {
                    _blockCount++;
                }
            }
            catch (Exception e)
            {
                _isDelete = true;
                Terminate();
                Console.WriteLine(e.Message);
                return; ;
            }

            try
            {
                using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
                {
                    for (int blockNumber = 0; blockNumber < _blockCount; blockNumber++)
                    {
                        byte[] blockValue = br.ReadBytes(GZip.BUFFER_SIZE);

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

                    byte[] compressedBlock = GZip.Compress(blockValue);

                    if (!writerTaskPool.TrySet(blockNumber, compressedBlock))
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
            int counter = 0;

            int blockNumber = -1;
            byte[] blockValue = null;

            try
            {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    bw.Write(BitConverter.GetBytes(_sourceLength));
                    bw.Write(BitConverter.GetBytes(_blockCount));

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

                        try
                        {
                            bw.Write(BitConverter.GetBytes(blockNumber));
                            bw.Write(blockValue.Length);
                            bw.Write(blockValue);
                        }
                        catch (IOException e)
                        {
                            Terminate();
                            Console.WriteLine("ERROR: операция прервана ({0})", e.Message);
                            bw.Close();
                            File.Delete(destination);
                            return;
                        }

                        counter++;
                        ShowProgress((double)counter / _blockCount);

                        if (counter == _blockCount)
                        {
                            Terminate();
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
