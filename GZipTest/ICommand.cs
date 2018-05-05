namespace GZipTest
{
    interface ICommand
    {
        void Reader(string source, ref BlockPool blockPool);

        void Handler(ref BlockPool readBlockPool, ref BlockPool writeBlockPool);

        void Writer(string destination, ref BlockPool blockPool);
    }
}
