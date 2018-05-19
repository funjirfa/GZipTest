using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    public class Decompressor : ICommand
    {
        public event CancellationEventHandler Cancel;

        public event ProgressEventHandler ShowProgress;

        public void Reader(string source, ref TaskPool readerTaskPool)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                br.BaseStream.Position = 16;

                for (int count = 0; count < FileSettings.BlockCount; count++)
                {
                    int blockNumber = br.ReadInt32();
                    int blockLength = br.ReadInt32();
                    byte[] block = br.ReadBytes(blockLength);

                    try
                    {
                        if (!readerTaskPool.TrySet(blockNumber, block))
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

                int blockLength = GZip.BUFFER_SIZE;
                if (blockNumber == FileSettings.BlockCount - 1)
                {
                    blockLength = FileSettings.LastBlockLength;
                }

                try
                {
                    byte[] decompressedBlock = GZip.Decompress(blockValue, blockLength);

                    if (!writerTaskPool.TrySet(blockNumber, decompressedBlock))
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

            Dictionary<int, byte[]> buffer = new Dictionary<int, byte[]>();

            using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
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

                    buffer[blockNumber] = blockValue;

                    while (buffer.ContainsKey(counter))
                    {
                        try
                        {
                            bw.Write(buffer[counter]);
                            buffer.Remove(counter);
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
                    };
                }
            }
        }
    }
}
