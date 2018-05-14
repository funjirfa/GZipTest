namespace GZipTest
{
    public delegate void ProgressEventHandler(string command, double progress);

    interface ICommand
    {
        event ProgressEventHandler ShowProgress;

        void Reader(string source, ref BlockPool blockPool);

        void Handler(ref BlockPool readBlockPool, ref BlockPool writeBlockPool);

        void Writer(string destination, ref BlockPool blockPool);
    }
}
