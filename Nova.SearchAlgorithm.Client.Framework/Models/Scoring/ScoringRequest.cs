using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class ScoringRequest
    {
        public SearchHlaData DonorHla { get; set; }
        public SearchHlaData PatientHla { get; set; }
    }
}