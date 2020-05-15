namespace Atlas.HlaMetadataDictionary.Exceptions
{
    public class LocusNameException : HlaMetadataDictionaryException
    {
        public LocusNameException(string locusName) : base(new HlaInfo(locusName), $"{locusName} is not a supported locus.")
        {
        }
    }
}
