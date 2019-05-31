using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class MemoryCacheException : Exception
    {
        public MemoryCacheException(string message)
            : base(message)
        {
        }

        public MemoryCacheException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}