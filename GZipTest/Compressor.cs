using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    public class Compressor : ICommand
    {
        private long _originFileLength;

        private long _blockCount;

        private bool _isError = false;

        public void Reader(string source, ref BlockPool blockPool)
        {
            FileInfo fi = new FileInfo(source);
            _originFileLength = fi.Length;
            _blockCount = _originFileLength / GZip.BUFFER_SIZE;
            if (_blockCount % GZip.BUFFER_SIZE > 0)
            {
                _blockCount++;
            }

            using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                for (int blockNumber = 0; blockNumber < _blockCount; blockNumber++)
                {
                    if (_isError)
                    {
                        blockPool.Complete();
                        break;
                    }

                    blockPool.Enqueue(new KeyValuePair<int, byte[]>( blockNumber, br.ReadBytes(1048576) ));
                }
            }

            blockPool.Complete();
        }

        public void Handler(ref BlockPool readBlockPool, ref BlockPool writeBlockPool)
        {
            while (true)
            {
                if (_isError)
                {
                    break;
                }

                KeyValuePair<int, byte[]> block = readBlockPool.Dequeue();

                if (block.Value == null)
                {
                    break;
                }

                byte[] compressedBlock = GZip.Compress(block.Value);

                writeBlockPool.Enqueue(new KeyValuePair<int, byte[]>( block.Key, compressedBlock ));
            }
        }

        public void Writer(string destination, ref BlockPool blockPool)
        {
            int counter = 0;

            using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                bw.Write(BitConverter.GetBytes(_originFileLength));
                bw.Write(BitConverter.GetBytes(_blockCount));

                while (true)
                {
                    KeyValuePair<int, byte[]> block = blockPool.Dequeue();

                    if (block.Value == null)
                    {
                        break;
                    }

                    try
                    {
                        bw.Write(BitConverter.GetBytes(block.Key));
                        bw.Write(block.Value.Length);
                        bw.Write(block.Value);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: операция прервана ({0})", e.Message);
                        bw.Close();
                        File.Delete(destination);
                        blockPool.Complete();
                        _isError = true;
                        return;
                    }
                    
                    counter++;
                    blockPool.Progress("Compressing", (double) counter / _blockCount);

                    if (counter == _blockCount)
                    {
                        blockPool.Complete();
                    }
                }
            }
        }
    }
}
