using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using FluentAssertions;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Client.Models.Common.Results;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Confidence
{
    [TestFixture]
    public class ConfidenceServiceTests
    {
        // Unless specified otherwise, all tests will be at a shared locus + position, to reduce setup in the individual test cases
        private const Locus TestLocus = Locus.A;

        private IConfidenceService confidenceService;


        [SetUp]
        public void SetUp()
        {
            var hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            var confidenceCalculator = new ConfidenceCalculator();
            var scoringCache = new ScoringCache(
                new PersistentCacheProvider(new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>());

            confidenceService = new ConfidenceService(hlaCategorisationService, confidenceCalculator, scoringCache);
        }

        [Test]
        public void CalculateMatchConfidences_CalculatesMatchesForMultipleLoci()
        {
            const Locus typedLocus = Locus.B;
            const Locus untypedLocus = Locus.Drb1;

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();

            var metadataAtTypedLocus = new HlaScoringMetadataBuilder().AtLocus(typedLocus).Build();
            patientLookupResults = patientLookupResults.SetLocus(typedLocus, metadataAtTypedLocus);
            donorLookupResults = donorLookupResults.SetLocus(typedLocus, metadataAtTypedLocus);

            var orientations = new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Direct });

            var confidences = confidenceService.Score(orientations, patientLookupResults, donorLookupResults);

            confidences.GetLocus(typedLocus).LocusScore.BothPositions(c => c == MatchConfidence.Definite).Should().BeTrue();
            confidences.GetLocus(untypedLocus).LocusScore.BothPositions(c => c == MatchConfidence.Potential).Should().BeTrue();
        }

        [Test]
        public void CalculateMatchConfidences_WhenCrossOrientationProvided_ReturnsConfidenceForCrossPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry> { new("serology", SerologySubtype.Associated, true) };

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var patientLookupResultSingleAllele1 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var donorLookupResultSerology = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.One, patientLookupResultSingleAllele1);
            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.One, donorLookupResultSerology);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.Two, donorLookupResultSingleAllele);

            var orientations = new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Cross });
            var confidences = confidenceService.Score(orientations, patientLookupResults, donorLookupResults)
                .GetLocus(TestLocus).LocusScore;

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Potential
            confidences.Position1.Should().Be(MatchConfidence.Potential);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Definite
            confidences.Position2.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void CalculateMatchConfidences_WhenDirectOrientationProvided_ReturnsConfidenceForDirectPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry> { new("serology", SerologySubtype.Associated, true) };

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var patientLookupResultSingleAllele1 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var donorLookupResultSerology = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.One, patientLookupResultSingleAllele1);
            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.One, donorLookupResultSerology);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.Two, donorLookupResultSingleAllele);

            var orientations = new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Direct });
            var confidences = confidenceService
                .Score(orientations, patientLookupResults, donorLookupResults)
                .GetLocus(TestLocus).LocusScore;

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.Position1.Should().Be(MatchConfidence.Potential);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.Position2.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void
            CalculateMatchConfidences_WhenBothOrientationsProvided_AndBothOrientationsHaveSameEquivalentConfidence_ReturnsConfidenceForSingleOrientation()
        {
            var matchingSerologies = new List<SerologyEntry> { new("serology", SerologySubtype.Associated, true) };

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var patientLookupResultSingleAllele1 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var donorLookupResultSerology = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();


            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.One, patientLookupResultSingleAllele1);
            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.Two, patientLookupResultSingleAllele2);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.One, donorLookupResultSerology);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.Two, donorLookupResultSingleAllele);

            var orientations = new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Direct, MatchOrientation.Cross });
            var confidences = confidenceService.Score(orientations, patientLookupResults, donorLookupResults)
                .GetLocus(TestLocus)
                .LocusScore
                .ToEnumerable();

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            // As both confidence pairs are equivalent, it doesn't matter which position has which confidence, as long as they are not both the same
            confidences.Should().BeEquivalentTo(MatchConfidence.Definite, MatchConfidence.Potential);
        }

        [Test]
        public void CalculateMatchConfidences_WhenBothOrientationsProvided_AndOneOrientationHasHigherConfidence_ReturnsConfidenceForBestOrientation()
        {
            var matchingSerologies = new List<SerologyEntry> { new("serology", SerologySubtype.Associated, true) };

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var patientLookupResultSingleAllele = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSerology = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>();
            var donorLookupResultSerology = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.One, patientLookupResultSingleAllele);
            patientLookupResults = patientLookupResults.SetPosition(TestLocus, LocusPosition.Two, patientLookupResultSerology);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.One, donorLookupResultSerology);
            donorLookupResults = donorLookupResults.SetPosition(TestLocus, LocusPosition.Two, donorLookupResultSingleAllele);

            var orientations = new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Direct, MatchOrientation.Cross });
            var confidences = confidenceService.Score(orientations, patientLookupResults, donorLookupResults)
                .GetLocus(TestLocus)
                .LocusScore;

            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Potential
            // Direct confidence (P2: D2) is Potential, Cross (P2: D1) is Definite

            // Cross match has a higher confidence, so should be returned
            confidences.Position1.Should().Be(MatchConfidence.Potential);
            confidences.Position2.Should().Be(MatchConfidence.Definite);
        }
    }
}