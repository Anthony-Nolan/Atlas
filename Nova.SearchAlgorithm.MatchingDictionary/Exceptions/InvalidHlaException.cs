using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class InvalidHlaException : MatchingDictionaryException
    {
        public InvalidHlaException(string locus, string hlaName) : base($"HLA type: {locus} {hlaName} is invalid.")
        {
        }

        public InvalidHlaException(MatchLocus matchLocus, string hlaName) : this(matchLocus.ToString(), hlaName)
        {
        }
    }
}