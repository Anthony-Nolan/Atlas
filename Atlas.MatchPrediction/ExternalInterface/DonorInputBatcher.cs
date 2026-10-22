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
                })
                // By frequency set metadata, which is what picks a donor's haplotype frequency set, so a batch spans as
                // few sets as possible and the batch's donors run set by set. A match prediction worker keeps only a
                // few sets in memory: in matching-result order, a batch spanned many sets, and each replica loaded the
                // same sets again and again (ATL-476). A stable sort, and each donor's result depends only on its own
                // input, so no result changes.
                .OrderBy(d => d.DonorFrequencySetMetadata?.RegistryCode, StringComparer.Ordinal)
                .ThenBy(d => d.DonorFrequencySetMetadata?.EthnicityCode, StringComparer.Ordinal);

            return consolidatedDonorInputs.Batch(batchSize).Select(donorBatch => new MultipleDonorMatchProbabilityInput(request)
            {
                MatchProbabilityRequestId = Guid.NewGuid().ToString(),
                Donors = donorBatch.ToList()
            });
        }
    }
}