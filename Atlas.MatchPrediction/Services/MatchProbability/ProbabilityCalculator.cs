using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IProbabilityCalculator
    {
        decimal CalculateMatchProbability(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            ISet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> matchingPairs,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods);
    }

    public class ProbabilityCalculator : IProbabilityCalculator
    {
        public decimal CalculateMatchProbability(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            ISet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> matchingPairs,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods)
        {
            var sumOfMatchingLikelihoods = 
                matchingPairs.Select(p => genotypesLikelihoods[p.Item1] * genotypesLikelihoods[p.Item2]).Sum();

            var sumOfPatientLikelihoods = patientGenotypes.Select(d => genotypesLikelihoods[d]).Sum();
            var sumOfDonorLikelihoods = donorGenotypes.Select(d => genotypesLikelihoods[d]).Sum();

            return sumOfMatchingLikelihoods / (sumOfPatientLikelihoods * sumOfDonorLikelihoods);
        }
    }
}
