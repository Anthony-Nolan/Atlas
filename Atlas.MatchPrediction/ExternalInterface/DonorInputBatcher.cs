using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IDonorInputBatcher
    {
        IEnumerable<MultipleDonorMatchProbabilityInput> BatchDonorInputs(
            IdentifiedMatchProbabilityRequest request,
            IEnumerable<DonorInput> donorInputs,
            int batchSize
        );
    }

    internal class DonorInputBatcher : IDonorInputBatcher
    {
        public IEnumerable<MultipleDonorMatchProbabilityInput> BatchDonorInputs(
            IdentifiedMatchProbabilityRequest request,
            IEnumerable<DonorInput> donorInputs,
            int batchSize = 10)
        {
            // It is not exactly sorting, but it puts donors with same frequence set next to each other.
            var consolidatedDonorInputs = donorInputs
                .GroupBy(x => x.DonorFrequencySetMetadata)
                .SelectMany(x => x
                    .GroupBy(y => y.DonorHla.ToPhenotypeInfo())
                    .Select(group => new DonorInput
                    {
                        DonorHla = group.Key.ToPhenotypeInfoTransfer(),
                        DonorFrequencySetMetadata = x.Key,
                        DonorIds = group.SelectMany(d => d.DonorIds).ToList()
                    })
                );



            return consolidatedDonorInputs.Batch(batchSize).Select(donorBatch => new MultipleDonorMatchProbabilityInput(request)
            {
                MatchProbabilityRequestId = Guid.NewGuid().ToString(),
                Donors = donorBatch.ToList()
            });
        }
    }
}