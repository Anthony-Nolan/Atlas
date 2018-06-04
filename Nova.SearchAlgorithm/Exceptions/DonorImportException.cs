using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorImportException : Exception
    {
        public DonorImportException(string message)
            : base(message)
        {
        }

        public DonorImportException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}