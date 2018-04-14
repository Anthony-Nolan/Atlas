using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class HlaException : Exception
    {
        public HlaException()
        {
        }

        public HlaException(string message)
            : base(message)
        {
        }

        public HlaException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}