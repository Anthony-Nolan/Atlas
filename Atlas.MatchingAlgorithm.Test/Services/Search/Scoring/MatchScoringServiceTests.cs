﻿using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using Atlas.Client.Models.Common.Results;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring
{
    [TestFixture]
    public class MatchScoringServiceTests
    {
        private IHlaScoringMetadataService scoringMetadataService;
        private IGradingService gradingService;
        private IConfidenceService confidenceService;
        private IAntigenMatchingService antigenMatchingService;
        private IRankingService rankingService;
        private IMatchScoreCalculator matchScoreCalculator;
        private IScoreResultAggregator scoreResultAggregator;
        private IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher;

        private IMatchScoringService scoringService;

        [SetUp]
        public void SetUp()
        {
            scoringMetadataService = Substitute.For<IHlaScoringMetadataService>();
            gradingService = Substitute.For<IGradingService>();
            confidenceService = Substitute.For<IConfidenceService>();
            antigenMatchingService = Substitute.For<IAntigenMatchingService>();

            rankingService = Substitute.For<IRankingService>();
            matchScoreCalculator = Substitute.For<IMatchScoreCalculator>();
            scoreResultAggregator = Substitute.For<IScoreResultAggregator>();
            var hlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            matchingAlgorithmSearchTrackingDispatcher = Substitute.For<IMatchingAlgorithmSearchTrackingDispatcher>();

            rankingService.RankSearchResults(Arg.Any<IEnumerable<MatchAndScoreResult>>())
                .Returns(callInfo => (IEnumerable<MatchAndScoreResult>) callInfo.Args().First());

            gradingService.Score(default, default, default)
                .ReturnsForAnyArgs(new LociInfo<LocusScoreResult<MatchGrade>>(new LocusScoreResult<MatchGrade>(MatchGrade.Mismatch)));

            confidenceService.Score(default, default, default)
                .ReturnsForAnyArgs(new LociInfo<LocusScoreResult<MatchConfidence>>(new LocusScoreResult<MatchConfidence>(MatchConfidence.Mismatch)));

            antigenMatchingService.Score(default, default, default)
                .ReturnsForAnyArgs(new LociInfo<LocusScoreResult<bool?>>(new LocusScoreResult<bool?>(false)));

            var hlaMetadataDictionaryBuilder = new HlaMetadataDictionaryBuilder().Using(scoringMetadataService);

            scoringService = new MatchScoringService(
                hlaMetadataDictionaryBuilder,
                hlaVersionAccessor,
                gradingService,
                confidenceService,
                antigenMatchingService,
                rankingService,
                matchScoreCalculator,
                scoreResultAggregator,
                Substitute.For<IMatchingAlgorithmSearchLogger>(),
                Substitute.For<IDpb1TceGroupMatchCalculator>(),
                Substitute.For<ILogger>(),
                matchingAlgorithmSearchTrackingDispatcher
            );
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreNoLoci_ReturnsMatchResultsWithNoScoreResults()
        {
            var matchResults = new List<MatchResult>
            {
                new MatchResultBuilder().WithHlaAtLocus(Locus.A, "hla-a").Build(),
                new MatchResultBuilder().WithHlaAtLocus(Locus.B, "hla-b").Build()
            };

            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.MatchResults, matchResults)
                .Build();

            var results = await scoringService.ScoreMatchesAgainstPatientHla(request);

            results.Select(r => r.MatchResult).Should().BeEquivalentTo(matchResults);
            results.Select(r => r.ScoreResult).Should().AllBeEquivalentTo((ScoreResult) null);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreNoLoci_DoesNotFetchScoringMetadataForAnyResults()
        {
            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.DidNotReceive().GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreSomeLoci_FetchesScoringMetadataForAllMatches()
        {
            // 3 loci x 2 x 2 results (patient is untyped)
            const int expectedNumberOfFetches = 12;

            var scoringCriteria = ScoringCriteriaBuilder.New
                .With(x => x.LociToScore, new[] {Locus.A, Locus.B, Locus.Drb1})
                .Build();

            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.ScoringCriteria, scoringCriteria)
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.Received(expectedNumberOfFetches).GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreAllLoci_FetchesScoringMetadataForAllMatches()
        {
            // 6 loci x 2 x 2 results (patient is untyped)
            const int expectedNumberOfFetches = 24;

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.Received(expectedNumberOfFetches).GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_DoesNotFetchScoringDataForUntypedLociForResults()
        {
            const Locus locus = Locus.B;
            const string patientHlaAtLocus = "patient-hla-locus-B";

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.PatientHla, new PhenotypeInfoBuilder<string>().WithDataAt(locus, patientHlaAtLocus).Build().ToPhenotypeInfoTransfer())
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().WithHlaAtLocus(locus, null).Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.DidNotReceive().GetHlaMetadata(locus, Arg.Is<string>(s => s != patientHlaAtLocus), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreNoLoci_DoesNotFetchScoringMetadataForPatientHla()
        {
            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.PatientHla, new PhenotypeInfo<string>("hla").ToPhenotypeInfoTransfer())
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.DidNotReceive().GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreSomeLoci_FetchesScoringMetadataForPatientHla()
        {
            // 2 loci x 2 positions (no donor HLA)
            const int expectedNumberOfFetches = 4;

            var scoringCriteria = ScoringCriteriaBuilder.New
                .With(x => x.LociToScore, new[] {Locus.C, Locus.Dpb1})
                .Build();

            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.ScoringCriteria, scoringCriteria)
                .With(x => x.PatientHla, new PhenotypeInfo<string>("hla").ToPhenotypeInfoTransfer())
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.Received(expectedNumberOfFetches).GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ScoreAllLoci_FetchesScoringMetadataForPatientHla()
        {
            // 6 loci x 2 positions (no donor HLA)
            const int expectedNumberOfFetches = 12;

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.PatientHla, new PhenotypeInfo<string>("hla").ToPhenotypeInfoTransfer())
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.Received(expectedNumberOfFetches).GetHlaMetadata(Arg.Any<Locus>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_DoesNotFetchScoringDataForUntypedLociForPatient()
        {
            const Locus untypedLocus = Locus.B;
            var patientHla = new PhenotypeInfoBuilder<string>("hla").WithDataAt(untypedLocus, (string) default).Build();

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.PatientHla, patientHla.ToPhenotypeInfoTransfer())
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            await scoringMetadataService.DidNotReceive().GetHlaMetadata(untypedLocus, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_DoesNotModifyMatchDetailsForResults()
        {
            var matchResult = new MatchResultBuilder().Build();

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] {matchResult})
                .Build();

            var results = await scoringService.ScoreMatchesAgainstPatientHla(request);

            results.First().MatchResult.Should().BeEquivalentTo(matchResult);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ReturnsMatchGradeForMatchResults()
        {
            const MatchGrade defaultMatchGrade = MatchGrade.PGroup;
            const MatchGrade expectedMatchGradeAtA1 = MatchGrade.GGroup;
            const MatchGrade expectedMatchGradeAtB2 = MatchGrade.Protein;

            var matchGrades = new LociInfoBuilder<LocusScoreResult<MatchGrade>>(new LocusScoreResult<MatchGrade>(defaultMatchGrade))
                .WithDataAt(Locus.A, new LocusScoreResult<MatchGrade>(expectedMatchGradeAtA1))
                .WithDataAt(Locus.B, new LocusScoreResult<MatchGrade>(expectedMatchGradeAtB2))
                .Build();

            gradingService.Score(default, default, default).ReturnsForAnyArgs(matchGrades);

            var results = await scoringService.ScoreMatchesAgainstPatientHla(
                MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            // Check across multiple loci and positions
            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchGrade.Should().Be(expectedMatchGradeAtA1);
            result.ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchGrade.Should().Be(expectedMatchGradeAtB2);
            result.ScoreResult.ScoreDetailsAtLocusDrb1.ScoreDetailsAtPosition1.MatchGrade.Should().Be(defaultMatchGrade);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_CalculatesMatchGradeForEachMatchResult()
        {
            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            gradingService.ReceivedWithAnyArgs(2).Score(default, default, default);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ReturnsMatchGradeScoreForMatchResults()
        {
            const MatchGrade defaultMatchGrade = MatchGrade.PGroup;
            const MatchGrade matchGradeAtA1 = MatchGrade.GGroup;
            const MatchGrade matchGradeAtB2 = MatchGrade.Protein;

            var matchGrades = new LociInfoBuilder<LocusScoreResult<MatchGrade>>(new LocusScoreResult<MatchGrade>(defaultMatchGrade))
                .WithDataAt(Locus.A, new LocusScoreResult<MatchGrade>(matchGradeAtA1))
                .WithDataAt(Locus.B, new LocusScoreResult<MatchGrade>(matchGradeAtB2))
                .Build();
            gradingService.Score(default, default, default).ReturnsForAnyArgs(matchGrades);

            const int expectedMatchGradeScoreAtA1 = 190;
            const int expectedMatchGradeScoreAtB2 = 87;
            matchScoreCalculator
                .CalculateScoreForMatchGrade(Arg.Is<MatchGrade>(a => a == matchGradeAtA1))
                .Returns(expectedMatchGradeScoreAtA1);
            matchScoreCalculator
                .CalculateScoreForMatchGrade(Arg.Is<MatchGrade>(a => a == matchGradeAtB2))
                .Returns(expectedMatchGradeScoreAtB2);

            var results = await scoringService.ScoreMatchesAgainstPatientHla(
                MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            // Check across multiple loci and positions
            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchGradeScore.Should().Be(expectedMatchGradeScoreAtA1);
            result.ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchGradeScore.Should().Be(expectedMatchGradeScoreAtB2);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ReturnsMatchConfidenceForMatchResults()
        {
            const MatchConfidence defaultConfidence = MatchConfidence.Potential;
            const MatchConfidence matchConfidenceAtA = MatchConfidence.Mismatch;
            const MatchConfidence matchConfidenceAtB = MatchConfidence.Definite;

            var confidences = new LociInfoBuilder<LocusScoreResult<MatchConfidence>>(new LocusScoreResult<MatchConfidence>(defaultConfidence))
                .WithDataAt(Locus.A, new LocusScoreResult<MatchConfidence>(matchConfidenceAtA))
                .WithDataAt(Locus.B, new LocusScoreResult<MatchConfidence>(matchConfidenceAtB))
                .Build();
            confidenceService.Score(null, null, null)
                .ReturnsForAnyArgs(confidences);

            var results = await scoringService.ScoreMatchesAgainstPatientHla(
                MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            // Check across multiple loci and positions
            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchConfidence.Should().Be(matchConfidenceAtA);
            result.ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchConfidence.Should().Be(matchConfidenceAtB);
            result.ScoreResult.ScoreDetailsAtLocusDrb1.ScoreDetailsAtPosition1.MatchConfidence.Should().Be(defaultConfidence);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ReturnsMatchConfidenceScoreForMatchResults()
        {
            const MatchConfidence defaultConfidence = MatchConfidence.Potential;
            const MatchConfidence matchConfidenceAtA1 = MatchConfidence.Mismatch;
            const MatchConfidence matchConfidenceAtB2 = MatchConfidence.Definite;

            var confidences = new LociInfoBuilder<LocusScoreResult<MatchConfidence>>(new LocusScoreResult<MatchConfidence>(defaultConfidence))
                .WithDataAt(Locus.A, new LocusScoreResult<MatchConfidence>(matchConfidenceAtA1))
                .WithDataAt(Locus.B, new LocusScoreResult<MatchConfidence>(matchConfidenceAtB2))
                .Build();
            confidenceService.Score(null, null, null)
                .ReturnsForAnyArgs(confidences);

            const int expectedMatchConfidenceScoreAtA1 = 7;
            const int expectedMatchConfidenceScoreAtB2 = 340;
            matchScoreCalculator
                .CalculateScoreForMatchConfidence(Arg.Is<MatchConfidence>(a => a == matchConfidenceAtA1))
                .Returns(expectedMatchConfidenceScoreAtA1);
            matchScoreCalculator
                .CalculateScoreForMatchConfidence(Arg.Is<MatchConfidence>(a => a == matchConfidenceAtB2))
                .Returns(expectedMatchConfidenceScoreAtB2);

            var results = await scoringService.ScoreMatchesAgainstPatientHla(
                MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            // Check across multiple loci and positions
            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchConfidenceScore.Should()
                .Be(expectedMatchConfidenceScoreAtA1);
            result.ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchConfidenceScore.Should()
                .Be(expectedMatchConfidenceScoreAtB2);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_CalculatesMatchConfidenceForEachMatchResult()
        {
            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            confidenceService.ReceivedWithAnyArgs(2).Score(null, null, null);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_ReturnsIsAntigenMatchForMatchResults()
        {
            const bool expectedValueAtA1 = true;
            const bool expectedValueAtB2 = false;

            var antigenMatches = new LociInfoBuilder<LocusScoreResult<bool?>>(new LocusScoreResult<bool?>(null))
                .WithDataAt(Locus.A, new LocusScoreResult<bool?>(expectedValueAtA1))
                .WithDataAt(Locus.B, new LocusScoreResult<bool?>(expectedValueAtB2))
                .Build();
            antigenMatchingService.Score(default, default, default)
                .ReturnsForAnyArgs(antigenMatches);

            var results = await scoringService.ScoreMatchesAgainstPatientHla(
                MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            // Check across multiple loci and positions
            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.IsAntigenMatch.Should().Be(expectedValueAtA1);
            result.ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.IsAntigenMatch.Should().Be(expectedValueAtB2);
            result.ScoreResult.ScoreDetailsAtLocusDrb1.ScoreDetailsAtPosition1.IsAntigenMatch.Should().BeNull();
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_CalculatesIsAntigenMatchForEachMatchResult()
        {
            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] { new MatchResultBuilder().Build(), new MatchResultBuilder().Build() })
                .Build();

            await scoringService.ScoreMatchesAgainstPatientHla(request);

            antigenMatchingService.ReceivedWithAnyArgs(2).Score(default, default, default);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_OnlyReturnsDetailsForScoredLoci()
        {
            const Locus scoredLocus = Locus.A;
            const Locus notScoredLocus = Locus.B;

            var scoringCriteria = ScoringCriteriaBuilder.New
                .With(x => x.LociToScore, new[] {scoredLocus})
                .Build();

            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build()})
                .With(x => x.ScoringCriteria, scoringCriteria)
                .Build();

            var results = await scoringService.ScoreMatchesAgainstPatientHla(request);

            var result = results.Single();
            result.ScoreResult.ScoreDetailsForLocus(scoredLocus).Should().NotBeNull();
            result.ScoreResult.ScoreDetailsForLocus(notScoredLocus).Should().BeNull();
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_SetsTypingStatusAtScoredLoci()
        {
            var lociToScore = new List<Locus> {Locus.A, Locus.C};

            var scoringCriteria = ScoringCriteriaBuilder.New
                .With(x => x.LociToScore, lociToScore)
                .Build();

            var donorWithUntypedLoci = new MatchResultBuilder()
                .WithHlaAtLocus(Locus.A, "hla-a")
                .WithHlaAtLocus(Locus.C, null)
                .Build();

            var request = MatchResultsScoringRequestBuilder.New
                .With(x => x.ScoringCriteria, scoringCriteria)
                .With(x => x.MatchResults, new[] {donorWithUntypedLoci})
                .Build();

            var results = await scoringService.ScoreMatchesAgainstPatientHla(request);

            var result = results.Single();
            result.ScoreResult.ScoreDetailsAtLocusA.IsLocusTyped.Should().BeTrue();
            result.ScoreResult.ScoreDetailsAtLocusC.IsLocusTyped.Should().BeFalse();
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_RanksResults()
        {
            var expectedSortedResults = new List<MatchAndScoreResult> {new MatchAndScoreResultBuilder().Build()};
            rankingService.RankSearchResults(null).ReturnsForAnyArgs(expectedSortedResults);

            var results =
                await scoringService.ScoreMatchesAgainstPatientHla(MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            results.Should().BeEquivalentTo(expectedSortedResults);
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_AggregatesScoringData()
        {
            await scoringService.ScoreMatchesAgainstPatientHla(MatchResultsScoringRequestBuilder.ScoreDefaultMatchAtAllLoci.Build());

            scoreResultAggregator.Received().AggregateScoreDetails(Arg.Any<ScoreResultAggregatorParameters>());
        }

        [Test]
        public async Task ScoreMatchesAgainstPatientHla_AssignsAggregateScoringData()
        {
            scoreResultAggregator.AggregateScoreDetails(Arg.Any<ScoreResultAggregatorParameters>()).Returns(new AggregateScoreDetails());

            var request = MatchResultsScoringRequestBuilder.ScoreAtAllLoci
                .With(x => x.MatchResults, new[] {new MatchResultBuilder().Build(), new MatchResultBuilder().Build()})
                .Build();

            var results = await scoringService.ScoreMatchesAgainstPatientHla(request);

            foreach (var result in results.Select(r => r.ScoreResult))
            {
                result.AggregateScoreDetails.Should().NotBeNull();
            }
        }
    }
}