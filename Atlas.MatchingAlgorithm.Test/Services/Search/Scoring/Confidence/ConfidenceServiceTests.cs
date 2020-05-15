using System.Collections.Generic;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using FluentAssertions;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Test.Builders;
using Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Scoring.Confidence
{
    [TestFixture]
    public class ConfidenceServiceTests
    {
        // Unless specified otherwise, all tests will be at a shared locus + position, to reduce setup in the individual test cases
        private const Locus Locus = Atlas.Common.GeneticData.Locus.A;
        private const TypePosition Position = TypePosition.One;

        private IConfidenceService confidenceService;

        private readonly MatchGradeResult defaultGradingResult = new MatchGradeResult
            {Orientations = new List<MatchOrientation> {MatchOrientation.Direct}};

        private PhenotypeInfo<MatchGradeResult> defaultGradingResults;

        [SetUp]
        public void SetUp()
        {
            var confidenceCalculator = new ConfidenceCalculator();
            var scoringCache = new ScoringCache(
                new PersistentCacheProvider(new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))),
                Substitute.For<IActiveHlaVersionAccessor>());

            confidenceService = new ConfidenceService(confidenceCalculator, scoringCache);
            defaultGradingResults = new PhenotypeInfo<MatchGradeResult>(defaultGradingResult);
        }

        [Test]
        public void CalculateMatchConfidences_CalculatesMatchesForMultipleLoci()
        {
            const Locus locus1 = Locus.B;
            const Locus locus2 = Locus.Drb1;

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().Build();
            patientLookupResults.SetAtPosition(locus1, Position, patientLookupResultAtLocus1);
            patientLookupResults.SetAtPosition(locus2, Position, null);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().Build();
            donorLookupResults.SetAtPosition(locus1, Position, donorLookupResultAtLocus1);
            donorLookupResults.SetAtPosition(locus2, Position, null);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.DataAtPosition(locus1, Position).Should().Be(MatchConfidence.Definite);
            confidences.DataAtPosition(locus2, Position).Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateMatchConfidences_WhenCrossOrientationProvided_ReturnsConfidenceForCrossPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry> {new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults.SetAtPosition(Locus, TypePosition.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePosition.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePosition.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.DataAtPosition(Locus, TypePosition.One).Should().Be(MatchConfidence.Definite);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.DataAtPosition(Locus, TypePosition.Two).Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateMatchConfidences_WhenDirectOrientationProvided_ReturnsConfidenceForDirectPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry> {new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults.SetAtPosition(Locus, TypePosition.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePosition.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePosition.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.DataAtPosition(Locus, TypePosition.One).Should().Be(MatchConfidence.Potential);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.DataAtPosition(Locus, TypePosition.Two).Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void
            CalculateMatchConfidences_WhenBothOrientationsProvided_AndBothOrientationsHaveSameEquivalentConfidence_ReturnsConfidenceForSingleOrientation()
        {
            var matchingSerologies = new List<SerologyEntry> {new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults.SetAtPosition(Locus, TypePosition.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePosition.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePosition.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position,
                new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct, MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            var confidencesAtLocus = new List<MatchConfidence>
            {
                confidences.DataAtPosition(Locus, TypePosition.One),
                confidences.DataAtPosition(Locus, TypePosition.Two)
            };

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential

            // As both confidence pairs are equivalent, it doesn't matter which position has which confidence, as long as they are not both the same
            confidencesAtLocus.Should().Contain(MatchConfidence.Definite);
            confidencesAtLocus.Should().Contain(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateMatchConfidences_WhenBothOrientationsProvided_AndOneOrientationHasHigherConfidence_ReturnsConfidenceForBestOrientation()
        {
            var matchingSerologies = new List<SerologyEntry> {new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            patientLookupResults.SetAtPosition(Locus, TypePosition.One, patientLookupResultSingleAllele);
            patientLookupResults.SetAtPosition(Locus, TypePosition.Two, patientLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePosition.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePosition.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position,
                new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct, MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Potential, Cross (P2: D1) is Potential

            // Cross match has a higher confidence, so should be returned
            confidences.DataAtPosition(Locus, TypePosition.One).Should().Be(MatchConfidence.Definite);
            confidences.DataAtPosition(Locus, TypePosition.Two).Should().Be(MatchConfidence.Potential);
        }
    }
}