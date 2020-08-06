using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.ExternalInterface
{
    internal interface IDonorInputBatcher
    {
        IEnumerable<MultipleDonorMatchProbabilityInput> BatchDonorInputs(
            MatchProbabilityRequestInput requestInput,
            IEnumerable<DonorInput> donorInputs,
            int batchSize
        );
    }

    internal class DonorInputBatcher : IDonorInputBatcher
    {
        public IEnumerable<MultipleDonorMatchProbabilityInput> BatchDonorInputs(
            MatchProbabilityRequestInput requestInput,
            IEnumerable<DonorInput> donorInputs,
            int batchSize = 10)
        {
            var consolidatedDonorInputs = donorInputs
                .GroupBy(d => new {d.DonorHla, d.DonorFrequencySetMetadata})
                .Select(group => new DonorInput
                {
                    DonorHla = group.Key.DonorHla,
                    DonorFrequencySetMetadata = group.Key.DonorFrequencySetMetadata,
                    DonorIds = group.SelectMany(d => d.DonorIds).ToList()
                });

            return consolidatedDonorInputs.Batch(batchSize).Select(donorBatch => new MultipleDonorMatchProbabilityInput(requestInput)
            {
                Donors = donorBatch.ToList()
            });
        }
    }
}