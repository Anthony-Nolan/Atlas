namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class InvalidHlaException : MatchingDictionaryException
    {
        public InvalidHlaException(HlaInfo hlaInfo) : base(hlaInfo, $"HLA type: {hlaInfo.Locus} {hlaInfo.HlaName} is invalid.")
        {
        }
    }
}