using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation
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
        private readonly string haplotypeFrequenciesDataSource;

        public NormalisedPoolGenerator(
            IHaplotypeFrequenciesReader reader, 
            INormalisedPoolRepository poolRepository,
            string haplotypeFrequenciesDataSource)
        {
            this.reader = reader;
            this.poolRepository = poolRepository;
            this.haplotypeFrequenciesDataSource = haplotypeFrequenciesDataSource;
        }

        public async Task<NormalisedHaplotypePool> GenerateNormalisedHaplotypeFrequencyPool()
        {
            var sourceData = await GetSourceData();

            if (sourceData.HaplotypeFrequencies.IsNullOrEmpty())
            {
                throw new Exception("No haplotype frequencies found - cannot generate normalised pool.");
            }

            return await GenerateNormalisedPool(sourceData);
        }

        private async Task<HaplotypeFrequenciesReaderResult> GetSourceData()
        {
            return await reader.GetUnalteredActiveGlobalHaplotypeFrequencies();
        }

        private async Task<NormalisedHaplotypePool> GenerateNormalisedPool(HaplotypeFrequenciesReaderResult sourceData)
        {
            var lowestFrequency = LowestFrequency(sourceData.HaplotypeFrequencies);
            var poolMembers = new List<NormalisedPoolMember>();
            var lastUpperBoundary = -1;

            foreach (var haplotypeFrequency in sourceData.HaplotypeFrequencies)
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

            var poolId = await AddPoolToDatabase(sourceData, poolMembers);

            return new NormalisedHaplotypePool(poolId, sourceData.HlaNomenclatureVersion, poolMembers);
        }

        private static decimal LowestFrequency(IReadOnlyCollection<HaplotypeFrequencyFileRecord> haplotypeFrequencies)
        {
            return haplotypeFrequencies
                .OrderBy(s => s.Frequency)
                .First()
                .Frequency;
        }

        private static int CalculateCopyNumber(HaplotypeFrequencyFileRecord next, decimal lowestFrequency)
        {
            return (int)decimal.Round(next.Frequency / lowestFrequency);
        }

        /// <returns>Normalised Pool database Id</returns>
        private async Task<int> AddPoolToDatabase(HaplotypeFrequenciesReaderResult sourceData, IReadOnlyCollection<NormalisedPoolMember> poolMembers)
        {
            if (sourceData.HaplotypeFrequencySetId == null)
            {
                throw new ArgumentNullException();
            }

            var poolId = await poolRepository.AddNormalisedPool(sourceData.HaplotypeFrequencySetId.Value, haplotypeFrequenciesDataSource);

            // presently for logging purposes only
            await OverwritePoolInDatabase(poolId, poolMembers);

            return poolId;
        }

        private async Task OverwritePoolInDatabase(int poolId, IReadOnlyCollection<NormalisedPoolMember> poolMembers)
        {
            await poolRepository.TruncateNormalisedHaplotypeFrequencies();
            var haplotypeFrequencies = poolMembers.Select(p => MapToDatabaseModel(poolId, p)).ToList();
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
