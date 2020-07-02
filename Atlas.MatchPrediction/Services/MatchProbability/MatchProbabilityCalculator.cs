using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal interface IMatchProbabilityCalculator
    {
        MatchProbabilityResponse CalculateMatchProbability(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            ISet<GenotypeMatchDetails> matchingPairs,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods);
    }

    internal class MatchProbabilityCalculator : IMatchProbabilityCalculator
    {
        public MatchProbabilityResponse CalculateMatchProbability(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            ISet<GenotypeMatchDetails> matchingPairs,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods)
        {
            var sumOfPatientLikelihoods = patientGenotypes.Select(d => genotypesLikelihoods[d]).Sum();
            var sumOfDonorLikelihoods = donorGenotypes.Select(d => genotypesLikelihoods[d]).Sum();

            if (sumOfPatientLikelihoods == 0 || sumOfDonorLikelihoods == 0)
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 0m};
            }

            var allowedLoci = LocusSettings.MatchPredictionLoci.ToList();

            var probabilityPerLocus = new LociInfo<decimal?>().Map((locus, info) =>
            {
                if (!allowedLoci.Contains(locus))
                {
                    return (decimal?) null;
                }
                
                var twoOutOfTwoMatch = matchingPairs.Where(g => g.MatchCounts.GetLocus(locus) == 2);
                var sumOfTwoOutOfTwoLikelihoods = 
                    twoOutOfTwoMatch.Select(g => genotypesLikelihoods[g.PatientGenotype] * genotypesLikelihoods[g.DonorGenotype]).Sum();

                return sumOfTwoOutOfTwoLikelihoods / (sumOfPatientLikelihoods * sumOfDonorLikelihoods);
            });

            var sumOfMatchingLikelihoods = matchingPairs.Where(g => g.IsTenOutOfTenMatch)
                    .Select(g => genotypesLikelihoods[g.PatientGenotype] * genotypesLikelihoods[g.DonorGenotype]).Sum();

            var matchProbability = sumOfMatchingLikelihoods / (sumOfPatientLikelihoods * sumOfDonorLikelihoods);

            return new MatchProbabilityResponse {MatchProbabilityPerLocus = probabilityPerLocus, ZeroMismatchProbability = matchProbability};
        }
    }
}