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
            IEnumerable<GenotypeMatchDetails> patientDonorMatchDetails,
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
        public LociInfo<decimal?> OneMismatchProbabilityPerLocus { get; set; } = new LociInfo<decimal?>(0);
        public LociInfo<decimal?> TwoMismatchProbabilityPerLocus { get; set; } = new LociInfo<decimal?>(0);

        public MatchProbabilityEquationNumerators Add(MatchProbabilityEquationNumerators other)
        {
            ZeroMismatchProbability += other.ZeroMismatchProbability;
            OneMismatchProbability += other.OneMismatchProbability;
            TwoMismatchProbability += other.TwoMismatchProbability;
            ZeroMismatchProbabilityPerLocus = ZeroMismatchProbabilityPerLocus
                .Map((l, x) => x == null ? null : x + other.ZeroMismatchProbabilityPerLocus.GetLocus(l));
            OneMismatchProbabilityPerLocus = OneMismatchProbabilityPerLocus
                .Map((l, x) => x == null ? null : x + other.OneMismatchProbabilityPerLocus.GetLocus(l));
            TwoMismatchProbabilityPerLocus = TwoMismatchProbabilityPerLocus
                .Map((l, x) => x == null ? null : x + other.TwoMismatchProbabilityPerLocus.GetLocus(l));
            return this;
        }
    }

    internal class MatchProbabilityCalculator : IMatchProbabilityCalculator
    {
        public MatchProbabilityResponse CalculateMatchProbability(
            SubjectCalculatorInputs patientInfo,
            SubjectCalculatorInputs donorInfo,
            IEnumerable<GenotypeMatchDetails> patientDonorMatchDetails,
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
                MatchProbabilities = new MatchProbabilities
                {
                    ZeroMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.ZeroMismatchProbability)),
                    OneMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.OneMismatchProbability)),
                    TwoMismatchProbability = new Probability(MatchProbability(matchProbabilityNumerators.TwoMismatchProbability)),
                },
                MatchProbabilitiesPerLocus = new LociInfo<MatchProbabilityPerLocusResponse>().Map((locus, _) =>
                {
                    var zeroMismatch = matchProbabilityNumerators.ZeroMismatchProbabilityPerLocus.GetLocus(locus);
                    var oneMismatch = matchProbabilityNumerators.OneMismatchProbabilityPerLocus.GetLocus(locus);
                    var twoMismatch = matchProbabilityNumerators.TwoMismatchProbabilityPerLocus.GetLocus(locus);
                    return new MatchProbabilityPerLocusResponse(new MatchProbabilities
                    {
                        ZeroMismatchProbability = zeroMismatch.HasValue ? new Probability(MatchProbability(zeroMismatch.Value)) : null,
                        OneMismatchProbability = oneMismatch.HasValue ? new Probability(MatchProbability(oneMismatch.Value)) : null,
                        TwoMismatchProbability = twoMismatch.HasValue ? new Probability(MatchProbability(twoMismatch.Value)) : null
                    });
                })
            };
        }

        private static MatchProbabilityEquationNumerators CalculateEquationNumerators(
            IEnumerable<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> patientLikelihoods,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> donorLikelihoods
        )
        {
            return patientDonorMatchDetails.AsParallel()
                .Aggregate(
                    () => new MatchProbabilityEquationNumerators(),
                    (numerators, pair) => AggregateNumerators(allowedLoci, patientLikelihoods, donorLikelihoods, pair, numerators),
                    (total, thisThread) => total.Add(thisThread),
                    (finalTotal) => finalTotal
                );
        }

        private static MatchProbabilityEquationNumerators AggregateNumerators(
            ISet<Locus> allowedLoci,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> patientLikelihoods,
            IReadOnlyDictionary<PhenotypeInfo<string>, decimal> donorLikelihoods,
            GenotypeMatchDetails pair,
            MatchProbabilityEquationNumerators numerators)
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

            LociInfo<decimal?> CalculateMismatchProbabilityPerLocus(LociInfo<decimal?> mismatchProbabilityPerLocus, int mismatchesPerLocus)
            {
                return mismatchProbabilityPerLocus.Map((locus, n) =>
                {
                    if (!allowedLoci.Contains(locus))
                    {
                        return (decimal?) null;
                    }

                    if (pair.MatchCounts.GetLocus(locus) == 2 - mismatchesPerLocus)
                    {
                        return n + pairLikelihood;
                    }

                    return n;
                });
            }

            numerators.ZeroMismatchProbabilityPerLocus = CalculateMismatchProbabilityPerLocus(numerators.ZeroMismatchProbabilityPerLocus, 0);
            numerators.OneMismatchProbabilityPerLocus = CalculateMismatchProbabilityPerLocus(numerators.OneMismatchProbabilityPerLocus, 1);
            numerators.TwoMismatchProbabilityPerLocus = CalculateMismatchProbabilityPerLocus(numerators.TwoMismatchProbabilityPerLocus, 2);

            return numerators;
        }
    }
}