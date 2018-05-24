using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GZipTest
{
    public static class GZip
    {
        public const int BUFFER_SIZE = 1048576;

        public static byte[] Compress(byte[] block)
        {
            using (MemoryStream compressedBlock = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(compressedBlock, CompressionMode.Compress))
                {
                    zip.Write(block, 0, block.Length);
                }

                return compressedBlock.ToArray();
            }
        }

        public static byte[] Decompress(byte[] block)
        {
            byte[] decompressedBlock = new byte[BUFFER_SIZE];

            int size;

            using (MemoryStream compressedBlock = new MemoryStream(block))
            {
                using (GZipStream zip = new GZipStream(compressedBlock, CompressionMode.Decompress))
                {
                    size = zip.Read(decompressedBlock, 0, BUFFER_SIZE);
                }
            }

            return decompressedBlock.Take(size).ToArray();
        }
    }
}
