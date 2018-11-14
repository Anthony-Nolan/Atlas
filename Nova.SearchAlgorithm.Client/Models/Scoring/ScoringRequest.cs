using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Client.Models.Scoring
{
    public class ScoringRequest
    {
        public SearchHlaData DonorHla { get; set; }
        public SearchHlaData PatientHla { get; set; }
    }
}