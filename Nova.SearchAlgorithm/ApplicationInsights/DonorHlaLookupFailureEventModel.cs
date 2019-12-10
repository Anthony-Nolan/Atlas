using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorHlaLookupFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - could not lookup HLA in matching dictionary";

        public DonorHlaLookupFailureEventModel(DonorProcessingException<MatchingDictionaryException> exception) 
            : base(MessageName, exception, exception.FailedDonorInfo)
        {
            Properties.Add("Locus", exception.Exception.HlaInfo.Locus);
            Properties.Add("HlaName", exception.Exception.HlaInfo.HlaName);
        }
    }
}