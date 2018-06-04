namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class PermittedLocusException : MatchingDictionaryException
    {
        public PermittedLocusException(string locusName) : base($"{locusName} is not a permitted locus.")
        {
        }
    }
}
