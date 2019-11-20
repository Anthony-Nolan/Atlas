using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class MatchingDictionaryLookupFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - could not lookup HLA in matching dictionary";

        public MatchingDictionaryLookupFailureEventModel(
            MatchingDictionaryException exception, 
            string donorId) : base(MessageName, exception, donorId)
        {
            Properties.Add("Locus", exception.HlaInfo.Locus);
            Properties.Add("HlaName", exception.HlaInfo.HlaName);
        }
    }
}