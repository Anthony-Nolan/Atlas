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
using LochNessBuilder;
using NUnit.Framework;

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

        private Builder<GenotypeMatchDetails> DefaultMatchDetailsBuilder => GenotypeMatchDetailsBuilder.New
            .WithAvailableLoci(AllowedLoci);
        
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
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithDoubleMismatchAt(Locus.Dqb1, Locus.Drb1).Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(1, 1, matchingPairs, AllowedLoci);

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.25M, 0.25M);
            actualProbability.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithOneMismatch_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithSingleMismatchAt(Locus.Drb1).Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(1, 1, matchingPairs, AllowedLoci);

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.5M, 0.25M);
            actualProbability.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtSameLocus_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithDoubleMismatchAt(Locus.Drb1).Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
            };


            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(1, 1, matchingPairs, AllowedLoci);

            var expectedMatchProbabilityPerLocus = new LociInfo<decimal?>(0.5M, 0.5M, 0.5M, null, 0.5M, 0.25M);
            actualProbability.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0.25m);
            actualProbability.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedMatchProbabilityPerLocus);
        }

        [Test]
        public void CalculateMatchProbability_WhenLocusWithTwoMismatchesAtDifferentLoci_ReturnsMatchProbability()
        {
            var matchingPairs = new HashSet<GenotypeMatchDetails>
            {
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
                DefaultMatchDetailsBuilder
                    .WithGenotypes(defaultDonorHla2, defaultPatientHla2)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().WithSingleMismatchAt(Locus.B, Locus.C).Build())
                    .WithDonorGenotypeLikelihood(0.5m)
                    .WithPatientGenotypeLikelihood(0.5m)
                    .Build(),
            };


            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(1, 1, matchingPairs, AllowedLoci);

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
                DefaultMatchDetailsBuilder
                    .WithGenotypes(sharedHlaMatch, sharedHlaMatch)
                    .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                    .WithDonorGenotypeLikelihood(0.01m)
                    .WithPatientGenotypeLikelihood(0.03m)
                    .Build()
            };

            var actualProbability = matchProbabilityCalculator.CalculateMatchProbability(0.03m, 0.07m, matchingPairs, AllowedLoci);

            actualProbability.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.1428571428571428571428571429m);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("10M pairs runs in 15s")]
        public void PerformanceTest()
        {
            var matchingPairs = DefaultMatchDetailsBuilder
                .WithGenotypes(defaultDonorHla1, defaultPatientHla1)
                .WithMatchCounts(new MatchCountsBuilder().TenOutOfTen().Build())
                .WithDonorGenotypeLikelihood(0.5m)
                .WithPatientGenotypeLikelihood(0.5m)
                .Build(10_000_000)
                .ToHashSet();

            matchProbabilityCalculator.CalculateMatchProbability(1, 1, matchingPairs, AllowedLoci);
        }
    }
}