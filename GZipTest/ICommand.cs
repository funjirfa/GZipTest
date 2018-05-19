namespace GZipTest
{
    public delegate void CancellationEventHandler();

    public delegate void ProgressEventHandler(double progress);

    interface ICommand
    {
        event CancellationEventHandler Cancel;

        event ProgressEventHandler ShowProgress;

        void Reader(string source, ref TaskPool readerTaskPool);

        void Handler(ref TaskPool readerTaskPool, ref TaskPool writerTaskPool);

        void Writer(string destination, ref TaskPool writerTaskPool);
    }
}
