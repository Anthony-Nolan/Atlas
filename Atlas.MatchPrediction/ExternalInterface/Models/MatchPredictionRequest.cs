using System;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    /// <summary>
    /// Requests for one patient vs a set of donors
    /// </summary>
    public class BatchedMatchPredictionRequests : MatchProbabilityRequestBase
    {
        public IEnumerable<Donor> Donors { get; set; }
    }

    /// <summary>
    /// Request for one patient vs one donor
    /// </summary>
    public class MatchPredictionRequest : MatchProbabilityRequestBase
    {
        public Donor Donor { get; set; }
    }

    public class Donor
    {
        public int? Id { get; set; }
        public PhenotypeInfoTransfer<string> Hla { get; set; }
        public FrequencySetMetadata FrequencySetMetadata { get; set; }
    }

    public static class MatchPredictionRequestExtensions
    {
        public static IEnumerable<SingleDonorMatchProbabilityInput> ToSingleDonorMatchProbabilityInputs(this BatchedMatchPredictionRequests requestBatch)
        {
            return requestBatch.Donors.Select(donor => new SingleDonorMatchProbabilityInput(requestBatch)
            {
                Donor = donor.ToDonorInput()
            });
        }

        private static DonorInput ToDonorInput(this Donor donor)
        {
            return new DonorInput
            {
                DonorId = donor.Id ?? throw new ArgumentNullException(nameof(donor.Id), "No donor Id submitted"),
                DonorHla = donor.Hla,
                DonorFrequencySetMetadata = donor.FrequencySetMetadata
            };
        }
    }
}
