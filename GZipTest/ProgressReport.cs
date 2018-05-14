using System;

namespace GZipTest
{
    public class ProgressReport
    {
        public void ShowProgress(string command, double progress)
        {
            Console.Write("{0}:\t{1:P}\r", command, progress);
        }
    }
}
