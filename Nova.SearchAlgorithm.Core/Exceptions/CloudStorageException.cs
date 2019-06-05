using System;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class CloudStorageException : Exception
    {
        public CloudStorageException(string message)
            : base(message)
        {
        }

        public CloudStorageException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}