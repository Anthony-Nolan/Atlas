using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.MutableModels;
using Atlas.Common.Utils.Models;
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
            decimal sumOfPatientLikelihoods,
            decimal sumOfDonorLikelihoods,
            IEnumerable<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci);
    }

    /// <summary>
    /// Match probability calculation involves running the same equation multiple times with the same denominator, and different numerators
    /// depending on what probability is being calculated.
    ///
    /// To avoid iterating a large collection of patient/donor pairs multiple times, this class allows us to calculate all the numerators
    /// together in one pass of the collection.
    ///
    /// Mutable LociInfo are used internally, which dramatically improves performance of aggregating these values.
    /// </summary>
    internal class MatchProbabilityEquationNumerators
    {
        public MatchProbabilityEquationNumerators(ISet<Locus> allowedLoci)
        {
            var initialPerLocusProbabilities = new LociInfo<decimal?>().Map((l, _) => allowedLoci.Contains(l) ? 0m : (decimal?) null);
            ZeroMismatchProbabilityPerLocus = initialPerLocusProbabilities.ToMutableLociInfo();
            OneMismatchProbabilityPerLocus = initialPerLocusProbabilities.ToMutableLociInfo();
            TwoMismatchProbabilityPerLocus = initialPerLocusProbabilities.ToMutableLociInfo();
        }

        public decimal ZeroMismatchProbability { get; set; } = 0;
        public decimal OneMismatchProbability { get; set; } = 0;
        public decimal TwoMismatchProbability { get; set; } = 0;
        public MutableLociInfo<decimal?> ZeroMismatchProbabilityPerLocus { get; }
        public MutableLociInfo<decimal?> OneMismatchProbabilityPerLocus { get; }
        public MutableLociInfo<decimal?> TwoMismatchProbabilityPerLocus { get; }

        public MatchProbabilityEquationNumerators Add(MatchProbabilityEquationNumerators other)
        {
            ZeroMismatchProbability += other.ZeroMismatchProbability;
            OneMismatchProbability += other.OneMismatchProbability;
            TwoMismatchProbability += other.TwoMismatchProbability;

            ZeroMismatchProbabilityPerLocus.A += other.ZeroMismatchProbabilityPerLocus.A;
            ZeroMismatchProbabilityPerLocus.B += other.ZeroMismatchProbabilityPerLocus.B;
            ZeroMismatchProbabilityPerLocus.C += other.ZeroMismatchProbabilityPerLocus.C;
            ZeroMismatchProbabilityPerLocus.Dqb1 += other.ZeroMismatchProbabilityPerLocus.Dqb1;
            ZeroMismatchProbabilityPerLocus.Drb1 += other.ZeroMismatchProbabilityPerLocus.Drb1;

            OneMismatchProbabilityPerLocus.A += other.OneMismatchProbabilityPerLocus.A;
            OneMismatchProbabilityPerLocus.B += other.OneMismatchProbabilityPerLocus.B;
            OneMismatchProbabilityPerLocus.C += other.OneMismatchProbabilityPerLocus.C;
            OneMismatchProbabilityPerLocus.Dqb1 += other.OneMismatchProbabilityPerLocus.Dqb1;
            OneMismatchProbabilityPerLocus.Drb1 += other.OneMismatchProbabilityPerLocus.Drb1;

            TwoMismatchProbabilityPerLocus.A += other.TwoMismatchProbabilityPerLocus.A;
            TwoMismatchProbabilityPerLocus.B += other.TwoMismatchProbabilityPerLocus.B;
            TwoMismatchProbabilityPerLocus.C += other.TwoMismatchProbabilityPerLocus.C;
            TwoMismatchProbabilityPerLocus.Dqb1 += other.TwoMismatchProbabilityPerLocus.Dqb1;
            TwoMismatchProbabilityPerLocus.Drb1 += other.TwoMismatchProbabilityPerLocus.Drb1;

            return this;
        }
    }

    internal class MatchProbabilityCalculator : IMatchProbabilityCalculator
    {
        public static int numberOfGoodPairs = 0;
        public static int numberOfBadPairs = 0;
        
        public MatchProbabilityResponse CalculateMatchProbability(
            decimal sumOfPatientLikelihoods,
            decimal sumOfDonorLikelihoods,
            IEnumerable<GenotypeMatchDetails> patientDonorMatchDetails,
            ISet<Locus> allowedLoci)
        {
            if (sumOfPatientLikelihoods == 0 || sumOfDonorLikelihoods == 0)
            {
                throw new InvalidDataException("Cannot calculate match probability for unrepresented hla");
            }

            var denominator = sumOfDonorLikelihoods * sumOfPatientLikelihoods;
            decimal MatchProbability(decimal numerator) => numerator / denominator;

            var matchProbabilityNumerators = CalculateEquationNumerators(patientDonorMatchDetails, allowedLoci);

            Console.WriteLine($"Bad Pairs: {numberOfBadPairs}");
            Console.WriteLine($"Good Pairs: {numberOfGoodPairs}");
            Console.WriteLine($"Potential Percentage improvement: {(decimal) numberOfBadPairs / (decimal) (numberOfGoodPairs + numberOfBadPairs):P}");
            
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
                    var zeroMismatch = matchProbabilityNumerators.ZeroMismatchProbabilityPerLocus.ToLociInfo().GetLocus(locus);
                    var oneMismatch = matchProbabilityNumerators.OneMismatchProbabilityPerLocus.ToLociInfo().GetLocus(locus);
                    var twoMismatch = matchProbabilityNumerators.TwoMismatchProbabilityPerLocus.ToLociInfo().GetLocus(locus);
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
            ISet<Locus> allowedLoci
        )
        {
            return patientDonorMatchDetails
                .Aggregate(
                    new MatchProbabilityEquationNumerators(allowedLoci),
                    (numerators, pair) => AggregateNumerators(pair, numerators),
                    finalTotal => finalTotal
                );
        }

        private static MatchProbabilityEquationNumerators AggregateNumerators(
            GenotypeMatchDetails pair,
            MatchProbabilityEquationNumerators numerators)
        {
            var pairLikelihood = pair.PatientGenotypeLikelihood * pair.DonorGenotypeLikelihood;

            switch (pair.MismatchCount)
            {
                case 0:
                    numerators.ZeroMismatchProbability += pairLikelihood;
                    numberOfGoodPairs++;
                    break;
                case 1:
                    numerators.OneMismatchProbability += pairLikelihood;
                    numberOfGoodPairs++;
                    break;
                case 2:
                    numerators.TwoMismatchProbability += pairLikelihood;
                    numberOfGoodPairs++;
                    break;
                default:
                    numberOfBadPairs++;
                    break;
            }

            switch (pair.MatchCounts.A)
            {
                case 2:
                    numerators.ZeroMismatchProbabilityPerLocus.A += pairLikelihood;
                    break;
                case 1:
                    numerators.OneMismatchProbabilityPerLocus.A += pairLikelihood;
                    break;
                case 0:
                    numerators.TwoMismatchProbabilityPerLocus.A += pairLikelihood;
                    break;
            }

            switch (pair.MatchCounts.B)
            {
                case 2:
                    numerators.ZeroMismatchProbabilityPerLocus.B += pairLikelihood;
                    break;
                case 1:
                    numerators.OneMismatchProbabilityPerLocus.B += pairLikelihood;
                    break;
                case 0:
                    numerators.TwoMismatchProbabilityPerLocus.B += pairLikelihood;
                    break;
            }

            switch (pair.MatchCounts.C)
            {
                case 2:
                    numerators.ZeroMismatchProbabilityPerLocus.C += pairLikelihood;
                    break;
                case 1:
                    numerators.OneMismatchProbabilityPerLocus.C += pairLikelihood;
                    break;
                case 0:
                    numerators.TwoMismatchProbabilityPerLocus.C += pairLikelihood;
                    break;
            }

            switch (pair.MatchCounts.Dqb1)
            {
                case 2:
                    numerators.ZeroMismatchProbabilityPerLocus.Dqb1 += pairLikelihood;
                    break;
                case 1:
                    numerators.OneMismatchProbabilityPerLocus.Dqb1 += pairLikelihood;
                    break;
                case 0:
                    numerators.TwoMismatchProbabilityPerLocus.Dqb1 += pairLikelihood;
                    break;
            }

            switch (pair.MatchCounts.Drb1)
            {
                case 2:
                    numerators.ZeroMismatchProbabilityPerLocus.Drb1 += pairLikelihood;
                    break;
                case 1:
                    numerators.OneMismatchProbabilityPerLocus.Drb1 += pairLikelihood;
                    break;
                case 0:
                    numerators.TwoMismatchProbabilityPerLocus.Drb1 += pairLikelihood;
                    break;
            }

            return numerators;
        }
    }
}