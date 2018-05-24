namespace GZipTest
{
    public delegate void TerminationEventHandler();

    public delegate void ProgressEventHandler(double progress);

    interface ICommand
    {
        event TerminationEventHandler Terminate;

        event ProgressEventHandler ShowProgress;

        void Reader(string source, ref TaskPool readerTaskPool);

        void Handler(ref TaskPool readerTaskPool, ref TaskPool writerTaskPool);

        void Writer(string destination, ref TaskPool writerTaskPool);
    }
}
