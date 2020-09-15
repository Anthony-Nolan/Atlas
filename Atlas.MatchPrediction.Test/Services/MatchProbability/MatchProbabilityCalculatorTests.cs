using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.Common.Test.SharedTestHelpers.Builders.DictionaryBuilder;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityCalculatorTests
    {
        private IMatchProbabilityCalculator matchProbabilityCalculator;

        private readonly PhenotypeInfo<string> defaultDonorHla1 = new PhenotypeInfo<string>("donor-hla-1");
        private readonly PhenotypeInfo<string> defaultDonorHla2 = new PhenotypeInfo<string>("donor-hla-2");
        private readonly PhenotypeInfo<string> defaultPatientHla1 = new PhenotypeInfo<string>("patient-hla-1");
        private readonly PhenotypeInfo<string> defaultPatientHla2 = new PhenotypeInfo<string>("patient-hla-2");

        private static readonly ISet<Locus> AllowedLoci = new HashSet<Locus> {Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1};

        [SetUp]
        public void Setup()
        {
            matchProbabilityCalculator = new MatchProbabilityCalculator();
        }

        [Test]
        public void CalculateMatchProbability_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithDoubleMismatchAt(Locus.Dqb1, Locus.Drb1).Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
            };

            var likelihoods = DictionaryWithCommonValue(0.5m, defaultDonorHla1, defaultDonorHla2, defaultPatientHla1, defaultPatientHla2);

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultPatientHla1, defaultPatientHla2).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultDonorHla1, defaultDonorHla2).Build(),
                matchingPairs,
                AllowedLoci
            );

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.25M, 0.25M);
            actualProbability.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithOneMismatch_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithSingleMismatchAt(Locus.Drb1).Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
            };

            var likelihoods = DictionaryWithCommonValue(0.5m, defaultDonorHla1, defaultDonorHla2, defaultPatientHla1, defaultPatientHla2);

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultPatientHla1, defaultPatientHla2).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultDonorHla1, defaultDonorHla2).Build(),
                matchingPairs,
                AllowedLoci
            );

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.5M, 0.25M);
            actualProbability.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtSameLocus_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithDoubleMismatchAt(Locus.Drb1).Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
            };

            var likelihoods = DictionaryWithCommonValue(0.5m, defaultDonorHla1, defaultDonorHla2, defaultPatientHla1, defaultPatientHla2);

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultPatientHla1, defaultPatientHla2).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultDonorHla1, defaultDonorHla2).Build(),
                matchingPairs,
                AllowedLoci
            );

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.5M, 0.25M);
            actualProbability.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtDifferentLoci_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithSingleMismatchAt(Locus.B, Locus.C).Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build(),
            };

            var likelihoods = DictionaryWithCommonValue(0.5m, defaultDonorHla1, defaultDonorHla2, defaultPatientHla1, defaultPatientHla2);

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultPatientHla1, defaultPatientHla2).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultDonorHla1, defaultDonorHla2).Build(),
                matchingPairs,
                AllowedLoci
            );

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.25M, 0.25M, null, 0.5M, 0.5M);
            actualProbability.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenPatientAndDonorHaveDifferentLikelihoodsForSameGenotypes_UsesCorrectLikelihoods()
        {
            var sharedHlaMatch = new PhenotypeInfo<string>("shared-hla-match");
            var sharedHlaMismatch = new PhenotypeInfo<string>("shared-hla-mismatch");

            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                GenotypeMatchDetailsBuilder.New
                    .WithGenotypes(sharedHlaMatch, sharedHlaMatch)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithAvailableLoci(AllowedLoci)
                    .Build()
            };

            var donorLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal> {{sharedHlaMatch, 0.01m}, {sharedHlaMismatch, 0.02m}};
            var patientLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal> {{sharedHlaMatch, 0.03m}, {sharedHlaMismatch, 0.04m}};

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(donorLikelihoods).WithGenotypes(sharedHlaMatch, sharedHlaMismatch).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(patientLikelihoods).WithGenotypes(sharedHlaMatch, sharedHlaMismatch).Build(),
                matchingPairs,
                AllowedLoci
            );

            actualProbability.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.1428571428571428571428571429m);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("10M pairs runs in 23s")]
        public void PerformanceTest()
        {
            var matchingPairs = GenotypeMatchDetailsBuilder.New
                .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                .WithAvailableLoci(AllowedLoci)
                .Build(10_000_000).ToHashSet();
            
            var likelihoods = DictionaryWithCommonValue(0.5m, defaultDonorHla1, defaultDonorHla2, defaultPatientHla1, defaultPatientHla2);

            matchProbabilityCalculator.CalculateMatchProbability(
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultPatientHla1, defaultPatientHla2).Build(),
                SubjectCalculatorInputsBuilder.New.WithLikelihoods(likelihoods).WithGenotypes(defaultDonorHla1, defaultDonorHla2).Build(),
                matchingPairs,
                AllowedLoci
            );
        }
    }
}