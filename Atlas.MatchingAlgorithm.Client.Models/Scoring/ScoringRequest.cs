using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public class ScoringRequest
    {
        // TODO: ATLAS-236: Use PhenotypeInfo?
        public SearchHlaData DonorHla { get; set; }
        public SearchHlaData PatientHla { get; set; }
    }
}