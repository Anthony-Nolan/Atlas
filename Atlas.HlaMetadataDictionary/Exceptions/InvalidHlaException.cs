namespace Atlas.MatchingAlgorithm.MatchingDictionary.Exceptions
{
    public class InvalidHlaException : MatchingDictionaryException
    {
        public InvalidHlaException(HlaInfo hlaInfo) : base(hlaInfo, $"HLA type: {hlaInfo.Locus} {hlaInfo.HlaName} is invalid.")
        {
        }
    }
}