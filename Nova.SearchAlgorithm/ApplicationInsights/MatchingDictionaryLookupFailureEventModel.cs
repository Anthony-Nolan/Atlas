using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class MatchingDictionaryLookupFailureEventModel : EventModel
    {
        private const string MessageName = "Error processing donor - could not lookup HLA in matching dictionary";
        
        public MatchingDictionaryLookupFailureEventModel(
            MatchingDictionaryException exception, 
            string donorId) : base(MessageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("DonorId", donorId);
            Properties.Add("Locus", exception.HlaInfo.Locus);
            Properties.Add("HlaName", exception.HlaInfo.HlaName);
        }
    }
}