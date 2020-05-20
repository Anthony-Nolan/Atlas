namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class LocusNameException : HlaMetadataDictionaryException
    {
        public LocusNameException(string locusName) : base(locusName, "", $"{locusName} is not a supported locus.")
        {
        }
    }
}
