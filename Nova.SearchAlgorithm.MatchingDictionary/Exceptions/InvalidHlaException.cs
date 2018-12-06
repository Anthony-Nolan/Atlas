using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Exceptions
{
    public class InvalidHlaException : MatchingDictionaryException
    {
        public InvalidHlaException(string locus, string hlaName) : base($"HLA type: {locus} {hlaName} is invalid.")
        {
        }

        public InvalidHlaException(Locus locus, string hlaName) : this(locus.ToString(), hlaName)
        {
        }
    }
}