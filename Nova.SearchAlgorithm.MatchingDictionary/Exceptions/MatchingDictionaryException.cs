using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class MatchingDictionaryException : Exception
    {
        public HlaInfo HlaInfo { get; }

        public MatchingDictionaryException()
        {
        }

        public MatchingDictionaryException(HlaInfo hlaInfo, string message)
            : base(message)
        {
            HlaInfo = hlaInfo;
        }

        public MatchingDictionaryException(HlaInfo hlaInfo, string message, Exception inner)
            : base(message, inner)
        {
            HlaInfo = hlaInfo;
        }
    }
}
