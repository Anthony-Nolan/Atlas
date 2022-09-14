using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.Functions.Models
{
    public class MatchPredictionRequest : MatchProbabilityRequest
    {
        public int DonorId { get; set; }
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
        public FrequencySetMetadata DonorFrequencySetMetadata { get; set; }
    }

    public static class MatchPredictionRequestExtensions
    {
        public static SingleDonorMatchProbabilityInput ToSingleDonorMatchProbabilityInput(this MatchPredictionRequest request)
        {
            return new SingleDonorMatchProbabilityInput(request)
            {
                Donor = new DonorInput
                {
                    DonorId = request.DonorId,
                    DonorHla = request.DonorHla,
                    DonorFrequencySetMetadata = request.DonorFrequencySetMetadata
                }
            };
        }
    }
}
