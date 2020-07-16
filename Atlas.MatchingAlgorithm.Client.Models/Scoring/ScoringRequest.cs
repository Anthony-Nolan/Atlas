using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public abstract class ScoringRequest
    {
        public PhenotypeInfo<string> PatientHla { get; set; }
        public ScoringCriteria ScoringCriteria { get; set; }
    }

    public class DonorHlaScoringRequest : ScoringRequest
    {
        public PhenotypeInfo<string> DonorHla { get; set; }
    }
}