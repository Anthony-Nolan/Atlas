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
            ISet<PhenotypeInfo<string>> patientInfo,
            ISet<PhenotypeInfo<string>> donorInfo,
            ISet<GenotypeMatchDetails> patientDonorMatchDetails,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods);
    }

    internal class MatchProbabilityCalculator : IMatchProbabilityCalculator
    {
        public MatchProbabilityResponse CalculateMatchProbability(
            ISet<PhenotypeInfo<string>> patientInfo,
            ISet<PhenotypeInfo<string>> donorInfo,
            ISet<GenotypeMatchDetails> patientDonorMatchDetails,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods)
        {
            var sumOfPatientLikelihoods = patientInfo.Select(p => genotypesLikelihoods[p]).Sum();
            var sumOfDonorLikelihoods = donorInfo.Select(d => genotypesLikelihoods[d]).Sum();

            if (sumOfPatientLikelihoods == 0 || sumOfDonorLikelihoods == 0)
            {
                return new MatchProbabilityResponse
                {
                    ZeroMismatchProbability = 0m,
                    OneMismatchProbability = 0m,
                    TwoMismatchProbability = 0m,
                    ZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>
                        {A = 0m, B = 0m, C = 0m, Dpb1 = null, Dqb1 = 0m, Drb1 = 0m}
                };
            }

            var allowedLoci = LocusSettings.MatchPredictionLoci.ToList();

            var probabilityPerLocus = new LociInfo<decimal?>().Map((locus, info) =>
            {
                if (!allowedLoci.Contains(locus))
                {
                    return (decimal?) null;
                }
                
                var twoOutOfTwoMatches = patientDonorMatchDetails.Where(g => g.MatchCounts.GetLocus(locus) == 2);

                return CalculateProbability(sumOfPatientLikelihoods, sumOfDonorLikelihoods, twoOutOfTwoMatches, genotypesLikelihoods);  
            });

            //TODO: ATLAS-235: Remove hard coded match count numbers

            var tenOutOfTenMatches = patientDonorMatchDetails.Where(g => g.MatchCount == 10);
            var zeroMismatchProbability = CalculateProbability(sumOfPatientLikelihoods, sumOfDonorLikelihoods, tenOutOfTenMatches, genotypesLikelihoods);

            var singleMismatches = patientDonorMatchDetails.Where(g => g.MatchCount == 9);
            var singleMismatchProbability = CalculateProbability(sumOfPatientLikelihoods, sumOfDonorLikelihoods, singleMismatches, genotypesLikelihoods);

            var doubleMismatches = patientDonorMatchDetails.Where(g => g.MatchCount == 8);
            var doubleMismatchProbability = CalculateProbability(sumOfPatientLikelihoods, sumOfDonorLikelihoods, doubleMismatches, genotypesLikelihoods);

            return new MatchProbabilityResponse
            {
                ZeroMismatchProbabilityPerLocus = probabilityPerLocus,
                ZeroMismatchProbability = zeroMismatchProbability,
                OneMismatchProbability = singleMismatchProbability,
                TwoMismatchProbability = doubleMismatchProbability,
            };
        }

        private static decimal CalculateProbability(
            decimal patientLikelihood,
            decimal donorLikelihood,
            IEnumerable<GenotypeMatchDetails> matchingLikelihoods,
            Dictionary<PhenotypeInfo<string>, decimal> genotypesLikelihoods)
        {
            var sumOfMatchingLikelihoods =
                matchingLikelihoods.Select(g => genotypesLikelihoods[g.PatientGenotype] * genotypesLikelihoods[g.DonorGenotype]).Sum();

            return sumOfMatchingLikelihoods / (patientLikelihood * donorLikelihood);
        }
    }
}