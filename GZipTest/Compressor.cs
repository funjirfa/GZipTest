using System;
using System.IO;

namespace GZipTest
{
    public class Compressor : ICommand
    {
        public event CancellationEventHandler Cancel;

        public event ProgressEventHandler ShowProgress;

        public void Reader(string source, ref TaskPool readerTaskPool)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                for (int blockNumber = 0; blockNumber < FileSettings.BlockCount; blockNumber++)
                {
                    int blockLength = GZip.BUFFER_SIZE;
                    if (blockNumber == FileSettings.BlockCount - 1)
                    {
                        blockLength = FileSettings.LastBlockLength;
                    }

                    try
                    {
                        if (!readerTaskPool.TrySet(blockNumber, br.ReadBytes(blockLength)))
                        {
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Cancel();
                        Console.WriteLine(e.Message);
                        return;
                    }
                }
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
                }
                catch (Exception e)
                {
                    Cancel();
                    Console.WriteLine(e.Message);
                    return;
                }

                if (blockValue == null)
                {
                    break;
                }

                try
                {
                    byte[] compressedBlock = GZip.Compress(blockValue);

                    if (!writerTaskPool.TrySet(blockNumber, compressedBlock))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    Cancel();
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

            using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                bw.Write(BitConverter.GetBytes(FileSettings.Length));
                bw.Write(BitConverter.GetBytes(FileSettings.BlockCount));

                while (true)
                {
                    try
                    {
                        if (!writerTaskPool.TryGet(out blockNumber, out blockValue))
                        {
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Cancel();
                        Console.WriteLine(e.Message);
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
                        Cancel();
                        Console.WriteLine("ERROR: операция прервана ({0})", e.Message);
                        bw.Close();
                        File.Delete(destination);
                        return;
                    }

                    counter++;

                    ShowProgress((double)counter / FileSettings.BlockCount);
                }
            }
        }
    }
}
