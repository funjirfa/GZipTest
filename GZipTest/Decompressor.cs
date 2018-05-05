using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    public class Decompressor : ICommand
    {
        private long _originFileLength;

        private long _blockCount;

        private bool _isError = false;

        private static int _lastBlockLength = GZip.BUFFER_SIZE;

        public void Reader(string source, ref BlockPool blockPool)
        {
            FileInfo compressedFile = new FileInfo(source);
            DriveInfo drive = new DriveInfo(compressedFile.Directory.Root.FullName);

            using (BinaryReader br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                _originFileLength = br.ReadInt64();

                if (_originFileLength > 4294967296 && drive.DriveFormat == "FAT32")
                {
                    _isError = true;
                    blockPool.Complete();
                    throw new IOException("ERROR: операция прервана (FAT32 - недостаточно места на диске)");
                }

                if (_originFileLength % GZip.BUFFER_SIZE > 0)
                {
                    _lastBlockLength = (int) _originFileLength % GZip.BUFFER_SIZE;
                }

                _blockCount = br.ReadInt64();

                for (int count = 0; count < _blockCount; count++)
                {
                    int blockNumber = br.ReadInt32();
                    int blockLength = br.ReadInt32();
                    byte[] block = br.ReadBytes(blockLength);
                    blockPool.Enqueue(new KeyValuePair<int, byte[]>(blockNumber, block));
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

                byte[] decompressedBlock = null;

                if (block.Key < _blockCount - 1)
                {
                    decompressedBlock = GZip.Decompress(block.Value, GZip.BUFFER_SIZE);
                }
                else
                {
                    decompressedBlock = GZip.Decompress(block.Value, _lastBlockLength);
                }

                writeBlockPool.Enqueue(new KeyValuePair<int, byte[]>(block.Key, decompressedBlock));
            }
        }

        public void Writer(string destination, ref BlockPool blockPool)
        {
            int counter = 0;

            if (_isError)
            {
                return;
            }

            using (BinaryWriter bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                while (true)
                {
                    KeyValuePair<int, byte[]> block = blockPool.Dequeue();

                    if (block.Value == null)
                    {
                        break;
                    }

                    try
                    {
                        bw.BaseStream.Position = block.Key * GZip.BUFFER_SIZE;
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
                    blockPool.Progress("Decompressing", (double) counter / _blockCount);

                    if (counter == _blockCount)
                    {
                        blockPool.Complete();
                    }
                }
            }
        }
    }
}
