using System;

namespace GZipTest
{
    public class ProgressReport
    {
        private readonly string Command;

        public ProgressReport(string command)
        {
            Command = command;
        }

        public void ShowProgress(double progress)
        {
            Console.Write("{0}: {1:P}\r", Command, progress);
        }

        public void Done(TimeSpan ts)
        {
            Console.Write("{0}: DONE! ({1:D2}:{2:D2}:{3:D2})", Command, ts.Hours, ts.Minutes, ts.Seconds);
        }
    }
}
