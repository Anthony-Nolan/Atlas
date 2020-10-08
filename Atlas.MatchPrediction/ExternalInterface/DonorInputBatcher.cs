using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IDonorInputBatcher
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
                .GroupBy(d => new
                {
                    // Convert to non-transfer PhenotypeInfo to ensure we use custom equality operators when grouping
                    Hla = d.DonorHla.ToPhenotypeInfo(),
                    d.DonorFrequencySetMetadata
                })
                .Select(group => new DonorInput
                {
                    DonorHla = group.Key.Hla.ToPhenotypeInfoTransfer(),
                    DonorFrequencySetMetadata = group.Key.DonorFrequencySetMetadata,
                    DonorIds = group.SelectMany(d => d.DonorIds).ToList()
                });

            return consolidatedDonorInputs.Batch(batchSize).Select(donorBatch => new MultipleDonorMatchProbabilityInput(requestInput)
            {
                MatchProbabilityRequestId = Guid.NewGuid().ToString(),
                Donors = donorBatch.ToList()
            });
        }
    }
}