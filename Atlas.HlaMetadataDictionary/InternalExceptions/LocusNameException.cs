using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;

namespace Atlas.HlaMetadataDictionary.InternalExceptions
{
    internal class LocusNameException : HlaMetadataDictionaryException
    {
        public LocusNameException(string locusName) : base(locusName, "", $"{locusName} is not a supported locus.")
        {
        }
    }
}
