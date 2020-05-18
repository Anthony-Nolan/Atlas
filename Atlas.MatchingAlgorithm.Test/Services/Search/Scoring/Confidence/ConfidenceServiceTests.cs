using System.Collections.Generic;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.ScoringInfo;
using FluentAssertions;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Confidence
{
    [TestFixture]
    public class ConfidenceServiceTests
    {
        // Unless specified otherwise, all tests will be at a shared locus + position, to reduce setup in the individual test cases
        private const Locus Locus = Atlas.Common.GeneticData.Locus.A;
        private const LocusPosition Position = LocusPosition.Position1;

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
            patientLookupResults.SetPosition(locus1, Position, patientLookupResultAtLocus1);
            patientLookupResults.SetPosition(locus2, Position, null);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().Build();
            donorLookupResults.SetPosition(locus1, Position, donorLookupResultAtLocus1);
            donorLookupResults.SetPosition(locus2, Position, null);

            var gradingResults = defaultGradingResults;
            gradingResults.SetPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.GetPosition(locus1, Position).Should().Be(MatchConfidence.Definite);
            confidences.GetPosition(locus2, Position).Should().Be(MatchConfidence.Potential);
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


            patientLookupResults.SetPosition(Locus, LocusPosition.Position1, patientLookupResultSingleAllele1);
            patientLookupResults.SetPosition(Locus, LocusPosition.Position2, patientLookupResultSingleAllele2);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position1, donorLookupResultSerology);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position2, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.GetPosition(Locus, LocusPosition.Position1).Should().Be(MatchConfidence.Definite);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.GetPosition(Locus, LocusPosition.Position2).Should().Be(MatchConfidence.Potential);
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


            patientLookupResults.SetPosition(Locus, LocusPosition.Position1, patientLookupResultSingleAllele1);
            patientLookupResults.SetPosition(Locus, LocusPosition.Position2, patientLookupResultSingleAllele2);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position1, donorLookupResultSerology);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position2, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetPosition(Locus, Position, new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.GetPosition(Locus, LocusPosition.Position1).Should().Be(MatchConfidence.Potential);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.GetPosition(Locus, LocusPosition.Position2).Should().Be(MatchConfidence.Definite);
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


            patientLookupResults.SetPosition(Locus, LocusPosition.Position1, patientLookupResultSingleAllele1);
            patientLookupResults.SetPosition(Locus, LocusPosition.Position2, patientLookupResultSingleAllele2);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position1, donorLookupResultSerology);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position2, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetPosition(Locus, Position,
                new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct, MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            var confidencesAtLocus = new List<MatchConfidence>
            {
                confidences.GetPosition(Locus, LocusPosition.Position1),
                confidences.GetPosition(Locus, LocusPosition.Position2)
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

            patientLookupResults.SetPosition(Locus, LocusPosition.Position1, patientLookupResultSingleAllele);
            patientLookupResults.SetPosition(Locus, LocusPosition.Position2, patientLookupResultSerology);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position1, donorLookupResultSerology);
            donorLookupResults.SetPosition(Locus, LocusPosition.Position2, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetPosition(Locus, Position,
                new MatchGradeResult {Orientations = new List<MatchOrientation> {MatchOrientation.Direct, MatchOrientation.Cross}});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Potential, Cross (P2: D1) is Potential

            // Cross match has a higher confidence, so should be returned
            confidences.GetPosition(Locus, LocusPosition.Position1).Should().Be(MatchConfidence.Definite);
            confidences.GetPosition(Locus, LocusPosition.Position2).Should().Be(MatchConfidence.Potential);
        }
    }
}