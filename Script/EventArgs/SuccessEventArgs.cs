using System;

namespace Korolow
{
    public class SuccessEventArgs : EventArgs
    {
        public string Result;
        public double Time;

        public SuccessEventArgs(string result, double time)
        {
            Result = result;
            Time = time;
        }
    }
}