using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

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