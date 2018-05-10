namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class InvalidHlaException : MatchingDictionaryException
    {
        public InvalidHlaException(string locus, string hlaName) : base($"HLA type: {locus} {hlaName} is invalid.")
        {
        }
    }
}
