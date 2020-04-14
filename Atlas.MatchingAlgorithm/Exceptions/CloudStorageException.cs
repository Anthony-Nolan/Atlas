using System;

namespace Atlas.MatchingAlgorithm.Exceptions
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