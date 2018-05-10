using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class MatchingDictionaryException : Exception
    {
        public MatchingDictionaryException()
        {
        }

        public MatchingDictionaryException(string message)
            : base(message)
        {
        }

        public MatchingDictionaryException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
