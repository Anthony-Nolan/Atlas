using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
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
        /// Note: the normalised frequencies are written to the database for logging purposes only; any existing data is overwritten.
        /// </summary>
        Task<NormalisedHaplotypePool> GenerateNormalisedHaplotypeFrequencyPool();
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

        public async Task<NormalisedHaplotypePool> GenerateNormalisedHaplotypeFrequencyPool()
        {
            var sourceData = await GetSourceData();

            if (sourceData.IsNullOrEmpty())
            {
                throw new Exception("No haplotype frequencies found - cannot generate normalised pool.");
            }

            var normalisedPool = await GenerateNormalisedPool(sourceData);

            // presently for logging purposes only
            await OverwritePoolInDatabase(normalisedPool);

            return normalisedPool;
        }

        private async Task<IReadOnlyCollection<HaplotypeFrequency>> GetSourceData()
        {
            return await reader.GetActiveGlobalHaplotypeFrequencies();
        }

        private async Task<NormalisedHaplotypePool> GenerateNormalisedPool(IReadOnlyCollection<HaplotypeFrequency> sourceData)
        {
            var lowestFrequency = sourceData.OrderBy(s => s.Frequency).First().Frequency;

            var poolMembers = new List<NormalisedPoolMember>();
            var lastUpperBoundary = -1;

            foreach (var haplotypeFrequency in sourceData)
            {
                var poolMember = new NormalisedPoolMember
                {
                    HaplotypeFrequency = haplotypeFrequency,
                    CopyNumber = CalculateCopyNumber(haplotypeFrequency, lowestFrequency),
                    PoolIndexLowerBoundary = 1 + lastUpperBoundary
                };

                poolMembers.Add(poolMember);
                lastUpperBoundary = poolMember.PoolIndexUpperBoundary;
            }

            var poolId = await poolRepository.AddNormalisedPool();

            return new NormalisedHaplotypePool(poolId, poolMembers);
        }

        private static int CalculateCopyNumber(HaplotypeFrequency next, decimal lowestFrequency)
        {
            return (int)decimal.Round(next.Frequency / lowestFrequency);
        }

        private async Task OverwritePoolInDatabase(NormalisedHaplotypePool pool)
        {
            await poolRepository.TruncateNormalisedHaplotypeFrequencies();
            var haplotypeFrequencies = pool.PoolMembers.Select(p => MapToDatabaseModel(pool.Id, p)).ToList();
            await poolRepository.BulkInsertNormalisedHaplotypeFrequencies(haplotypeFrequencies);
        }

        private static NormalisedHaplotypeFrequency MapToDatabaseModel(int poolId, NormalisedPoolMember poolMember)
        {
            return new NormalisedHaplotypeFrequency
            {
                NormalisedPool_Id = poolId,
                A = poolMember.HaplotypeFrequency.A,
                B = poolMember.HaplotypeFrequency.B,
                C = poolMember.HaplotypeFrequency.C,
                DQB1 = poolMember.HaplotypeFrequency.Dqb1,
                DRB1 = poolMember.HaplotypeFrequency.Drb1,
                Frequency = poolMember.HaplotypeFrequency.Frequency,
                CopyNumber = poolMember.CopyNumber
            };
        }
    }
}
