using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public abstract class ScoringRequest
    {
        public PhenotypeInfoTransfer<string> PatientHla { get; set; }
        public ScoringCriteria ScoringCriteria { get; set; }
    }

    public class DonorHlaScoringRequest : ScoringRequest
    {
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
    }
}