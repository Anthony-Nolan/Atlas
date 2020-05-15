using System;

namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class HlaMetadataDictionaryException : Exception
    {
        public HlaInfo HlaInfo { get; }

        public HlaMetadataDictionaryException(HlaInfo hlaInfo, string message)
            : base(message)
        {
            HlaInfo = hlaInfo;
        }

        public HlaMetadataDictionaryException(HlaInfo hlaInfo, string message, Exception inner)
            : base(message, inner)
        {
            HlaInfo = hlaInfo;
        }
    }
}
