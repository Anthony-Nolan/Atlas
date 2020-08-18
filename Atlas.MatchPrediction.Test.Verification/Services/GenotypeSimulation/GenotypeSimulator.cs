using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation
{
    internal interface IGenotypeSimulator
    {
        /// <param name="requiredGenotypeCount">Number of genotypes to be built.</param>
        /// <param name="pool">Data source for genotype simulation.</param>
        IReadOnlyCollection<PhenotypeInfo<string>> SimulateGenotypes(int requiredGenotypeCount, NormalisedHaplotypePool pool);
    }

    internal class GenotypeSimulator : IGenotypeSimulator
    {
        private readonly IRandomNumberGenerator randomNumberGenerator;

        public GenotypeSimulator(IRandomNumberGenerator randomNumberGenerator)
        {
            this.randomNumberGenerator = randomNumberGenerator;
        }
        
        public IReadOnlyCollection<PhenotypeInfo<string>> SimulateGenotypes(int requiredGenotypeCount, NormalisedHaplotypePool pool)
        {
            var request = new GenerateRandomNumberRequest
            {
                Count = requiredGenotypeCount,
                MinPermittedValue = 0,
                MaxPermittedValue = pool.TotalCopyNumber - 1
            };
            var pairs = randomNumberGenerator.GenerateRandomNumberPairs(request);

            return pairs.AsParallel().Select(p => BuildSimulatedHlaTyping(pool, p)).ToList();
        }

        private static PhenotypeInfo<string> BuildSimulatedHlaTyping(NormalisedHaplotypePool pool, UnorderedPair<int> indexPair)
        {
            var firstHaplotype = pool.GetHaplotypeFrequencyByPoolIndex(indexPair.Item1);
            var secondHaplotype = pool.GetHaplotypeFrequencyByPoolIndex(indexPair.Item2);

            return new PhenotypeInfo<string>(
                valueA: new LocusInfo<string>(firstHaplotype.A, secondHaplotype.A),
                valueB: new LocusInfo<string>(firstHaplotype.B, secondHaplotype.B),
                valueC: new LocusInfo<string>(firstHaplotype.C, secondHaplotype.C),
                valueDqb1: new LocusInfo<string>(firstHaplotype.Dqb1, secondHaplotype.Dqb1),
                valueDrb1: new LocusInfo<string>(firstHaplotype.Drb1, secondHaplotype.Drb1));
        }
    }
}
