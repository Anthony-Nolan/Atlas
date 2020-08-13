using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation
{
    internal interface IGenotypeSimulator
    {
        /// <param name="requiredGenotypeCount">Number of genotypes to be built.</param>
        /// <param name="pool">Data source for genotype simulation.</param>
        IReadOnlyCollection<SimulatedHlaTyping> SimulateGenotypes(int requiredGenotypeCount, NormalisedHaplotypePool pool);
    }

    internal class GenotypeSimulator : IGenotypeSimulator
    {
        private readonly IRandomNumberGenerator randomNumberGenerator;

        public GenotypeSimulator(IRandomNumberGenerator randomNumberGenerator)
        {
            this.randomNumberGenerator = randomNumberGenerator;
        }
        
        public IReadOnlyCollection<SimulatedHlaTyping> SimulateGenotypes(int requiredGenotypeCount, NormalisedHaplotypePool pool)
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

        private static SimulatedHlaTyping BuildSimulatedHlaTyping(NormalisedHaplotypePool pool, UnorderedPair<int> indexPair)
        {
            var firstHaplotype = pool.GetHaplotypeFrequencyByPoolIndex(indexPair.Item1);
            var secondHaplotype = pool.GetHaplotypeFrequencyByPoolIndex(indexPair.Item2);

            return new SimulatedHlaTyping
            {
                A_1 = firstHaplotype.A,
                A_2 = secondHaplotype.A,
                B_1 = firstHaplotype.B,
                B_2 = secondHaplotype.B,
                C_1 = firstHaplotype.C,
                C_2 = secondHaplotype.C,
                Dqb1_1 = firstHaplotype.Dqb1,
                Dqb1_2 = secondHaplotype.Dqb1,
                Drb1_1 = firstHaplotype.Drb1,
                Drb1_2 = secondHaplotype.Drb1
            };
        }
    }
}
