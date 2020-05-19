using System;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    internal class MemoryCacheException : Exception
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