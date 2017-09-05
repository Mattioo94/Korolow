using System;

namespace Korolow
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Error;
        
        public ErrorEventArgs(Exception error)
        {
            Error = error;
        }
    }
}