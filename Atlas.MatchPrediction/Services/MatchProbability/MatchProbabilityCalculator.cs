using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

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

    /// <summary>
    /// Match probability calculation involves running the same equation multiple times with the same denominator, and different numerators
    /// depending on what probability is being calculated.
    ///
    /// To avoid iterating a large collection of patient/donor pairs multiple times, this class allows us to calculate all the numerators
    /// together in one pass of the collection. 
    /// </summary>
    internal class MatchProbabilityEquationNumerators
    {
        public decimal ZeroMismatchProbability { get; set; } = 0;
        public decimal OneMismatchProbability { get; set; } = 0;
        public decimal TwoMismatchProbability { get; set; } = 0;
        public LociInfo<decimal?> ZeroMismatchProbabilityPerLocus { get; set; } = new LociInfo<decimal?>(0);
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

            var sumOfPatientLikelihoods = patientInfo.Genotypes.Select(p => patientLikelihoods[p]).SumDecimals();
            var sumOfDonorLikelihoods = donorInfo.Genotypes.Select(d => donorLikelihoods[d]).SumDecimals();

            if (sumOfPatientLikelihoods == 0 || sumOfDonorLikelihoods == 0)
            {
                throw new InvalidDataException("Cannot calculate match probability for unrepresented hla");
            }

            var denominator = sumOfDonorLikelihoods * sumOfPatientLikelihoods;
            decimal MatchProbability(decimal numerator) => numerator / denominator;

            var matchProbabilityNumerators = CalculateEquationNumerators(patientDonorMatchDetails, allowedLoci, patientLikelihoods, donorLikelihoods);

            return new MatchProbabilityResponse
            {
                ZeroMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.ZeroMismatchProbability)),
                OneMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.OneMismatchProbability)),
                TwoMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.TwoMismatchProbability)),
                ZeroMismatchProbabilityPerLocus = matchProbabilityNumerators.ZeroMismatchProbabilityPerLocus.Map((l, v) =>
                    v.HasValue ? new Probability(MatchProbability(v.Value)) : null
                )
            };
        }

        private static MatchProbabilityEquationNumerators CalculateEquationNumerators(
            ISet<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> patientLikelihoods,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> donorLikelihoods
        )
        {
            return patientDonorMatchDetails.Aggregate(new MatchProbabilityEquationNumerators(), (numerators, pair) =>
            {
                var pairLikelihood = patientLikelihoods[pair.PatientGenotype] * donorLikelihoods[pair.DonorGenotype];

                switch (pair.MismatchCount)
                {
                    case 0:
                        numerators.ZeroMismatchProbability += pairLikelihood;
                        break;
                    case 1:
                        numerators.OneMismatchProbability += pairLikelihood;
                        break;
                    case 2:
                        numerators.TwoMismatchProbability += pairLikelihood;
                        break;
                }

                numerators.ZeroMismatchProbabilityPerLocus = numerators.ZeroMismatchProbabilityPerLocus.Map((locus, n) =>
                {
                    if (!allowedLoci.Contains(locus))
                    {
                        return (decimal?) null;
                    }

                    if (pair.MatchCounts.GetLocus(locus) == 2)
                    {
                        return n + pairLikelihood;
                    }

                    return n;
                });

                return numerators;
            });
        }
    }
}