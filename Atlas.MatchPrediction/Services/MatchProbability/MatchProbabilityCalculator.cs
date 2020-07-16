using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal class SubjectCalculatorInputs
    {
        public ISet<PhenotypeInfo<string>> Genotypes { get; set; }
        public IReadOnlyDictionary<PhenotypeInfo<string>, decimal> GenotypeLikelihoods { get; set; }
    }

    internal interface IMatchProbabilityCalculator
    {
        MatchProbabilityResponse CalculateMatchProbability(
            SubjectCalculatorInputs patientInfo,
            SubjectCalculatorInputs donorInfo,
            ISet<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci);
    }

    internal class MatchProbabilityCalculator : IMatchProbabilityCalculator
    {
        public MatchProbabilityResponse CalculateMatchProbability(
            SubjectCalculatorInputs patientInfo,
            SubjectCalculatorInputs donorInfo,
            ISet<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci)
        {
            var patientLikelihoods = patientInfo.GenotypeLikelihoods;
            var donorLikelihoods = donorInfo.GenotypeLikelihoods;

            var sumOfPatientLikelihoods = patientInfo.Genotypes.Select(p => patientLikelihoods[p]).Sum();
            var sumOfDonorLikelihoods = donorInfo.Genotypes.Select(d => donorLikelihoods[d]).Sum();

            if (sumOfPatientLikelihoods == 0 || sumOfDonorLikelihoods == 0)
            {
                return new MatchProbabilityResponse(Probability.Zero(), allowedLoci);
            }

            decimal CalculateProbability(Func<ISet<GenotypeMatchDetails>, IEnumerable<GenotypeMatchDetails>> filterMatches)
            {
                var filteredMatches = filterMatches(patientDonorMatchDetails);
                var sumOfMatchingLikelihoods = filteredMatches.Sum(g => patientLikelihoods[g.PatientGenotype] * donorLikelihoods[g.DonorGenotype]);

                return sumOfMatchingLikelihoods / (sumOfPatientLikelihoods * sumOfDonorLikelihoods);
            }

            var probabilityPerLocus = new LociInfo<decimal?>().Map((locus, info) =>
            {
                if (!allowedLoci.Contains(locus))
                {
                    return (decimal?) null;
                }

                return CalculateProbability(m => m.Where(g => g.MatchCounts.GetLocus(locus) == 2));
            });

            var zeroMismatchProbability = CalculateProbability(m => m.Where(g => g.MismatchCount == 0));
            var singleMismatchProbability = CalculateProbability(m => m.Where(g => g.MismatchCount == 1));
            var twoMismatchProbability = CalculateProbability(m => m.Where(g => g.MismatchCount == 2));

            return new MatchProbabilityResponse
            {
                ZeroMismatchProbability = new Probability(zeroMismatchProbability),
                OneMismatchProbability = new Probability(singleMismatchProbability),
                TwoMismatchProbability = new Probability(twoMismatchProbability),
                ZeroMismatchProbabilityPerLocus = probabilityPerLocus.Map((l, v) => v.HasValue ? new Probability(v.Value) : null)
            };
        }
    }
}