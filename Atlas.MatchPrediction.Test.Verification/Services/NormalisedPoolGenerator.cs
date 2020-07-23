using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface INormalisedPoolGenerator
    {
        /// <summary>
        /// Generates the normalised haplotype frequency pool.
        /// Note: generated pool is written to database for logging purposes only; any existing data is overwritten.
        /// </summary>
        Task<IReadOnlyCollection<NormalisedHaplotypeFrequency>> GenerateNormalisedHaplotypeFrequencyPool();
    }

    internal class NormalisedPoolGenerator : INormalisedPoolGenerator
    {
        private readonly IHaplotypeFrequenciesReader reader;
        private readonly INormalisedPoolRepository poolRepository;

        public NormalisedPoolGenerator(IHaplotypeFrequenciesReader reader, INormalisedPoolRepository poolRepository)
        {
            this.reader = reader;
            this.poolRepository = poolRepository;
        }

        public async Task<IReadOnlyCollection<NormalisedHaplotypeFrequency>> GenerateNormalisedHaplotypeFrequencyPool()
        {
            var sourceData = await GetSourceData();

            if (sourceData.IsNullOrEmpty())
            {
                throw new Exception("No haplotype frequencies found - cannot generate normalised pool.");
            }

            var normalisedPool = GenerateNormalisedPool(sourceData);

            // presently for logging purposes only
            await OverwritePoolInDatabase(normalisedPool);

            return normalisedPool;
        }

        private async Task<IReadOnlyCollection<HaplotypeFrequency>> GetSourceData()
        {
            return await reader.GetActiveGlobalHaplotypeFrequencies();
        }

        private static IReadOnlyCollection<NormalisedHaplotypeFrequency> GenerateNormalisedPool(IReadOnlyCollection<HaplotypeFrequency> sourceData)
        {
            var lowestFrequency = sourceData.OrderBy(s => s.Frequency).First().Frequency;
            return sourceData
                .Select(h => NormaliseHaplotypesAgainstLowestFrequency(h, lowestFrequency))
                .ToList();
        }

        private static NormalisedHaplotypeFrequency NormaliseHaplotypesAgainstLowestFrequency(HaplotypeFrequency haplotypeFrequency, decimal lowestFrequency)
        {
            return new NormalisedHaplotypeFrequency
            {
                A = haplotypeFrequency.A,
                B = haplotypeFrequency.B,
                C = haplotypeFrequency.C,
                DQB1 = haplotypeFrequency.Dqb1,
                DRB1 = haplotypeFrequency.Drb1,
                Frequency = haplotypeFrequency.Frequency,
                CopyNumber = CalculateCopyNumber(haplotypeFrequency, lowestFrequency)
            };
        }

        private static int CalculateCopyNumber(HaplotypeFrequency next, decimal lowestFrequency)
        {
            return (int)decimal.Round(next.Frequency / lowestFrequency);
        }

        private async Task OverwritePoolInDatabase(IReadOnlyCollection<NormalisedHaplotypeFrequency> pool)
        {
            await poolRepository.DeleteNormalisedHaplotypeFrequencyPool();
            await poolRepository.BulkInsertNormalisedHaplotypes(pool);
        }
    }
}
