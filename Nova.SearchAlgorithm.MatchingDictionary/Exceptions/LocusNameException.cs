namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class LocusNameException : MatchingDictionaryException
    {
        public LocusNameException(string locusName) : base($"{locusName} is not a supported locus.")
        {
        }
    }
}
