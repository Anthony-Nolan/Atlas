namespace Atlas.MatchingAlgorithm.MatchingDictionary.Exceptions
{
    public class LocusNameException : MatchingDictionaryException
    {
        public LocusNameException(string locusName) : base(new HlaInfo(locusName), $"{locusName} is not a supported locus.")
        {
        }
    }
}
