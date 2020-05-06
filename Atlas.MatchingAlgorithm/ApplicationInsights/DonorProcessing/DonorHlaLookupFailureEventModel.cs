using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.HlaMetadataDictionary.Exceptions;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing
{
    public class DonorHlaLookupFailureEventModel : DonorProcessingFailureEventModel
    {
        public DonorHlaLookupFailureEventModel(string eventName, DonorProcessingException<MatchingDictionaryException> exception) 
            : base(eventName, exception.Exception, exception.FailedDonorInfo)
        {
            Properties.Add("Locus", exception.Exception.HlaInfo.Locus);
            Properties.Add("HlaName", exception.Exception.HlaInfo.HlaName);
        }
    }
}