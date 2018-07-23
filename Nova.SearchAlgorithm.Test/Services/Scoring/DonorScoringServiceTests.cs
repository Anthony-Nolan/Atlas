using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring
{
    [TestFixture]
    public class DonorScoringServiceTests
    {
        private IHlaScoringLookupService scoringLookupService;
        private IGradingService gradingService;
        private IConfidenceService confidenceService;
        private IRankingService rankingService;
        private IMatchScoreCalculator matchScoreCalculator;

        private DonorScoringService donorScoringService;

        private readonly PhenotypeInfo<MatchGradeResult> defaultMatchGradeResults = new PhenotypeInfo<MatchGradeResult>
        {
            A_1 = new MatchGradeResult(),
            A_2 = new MatchGradeResult(),
            B_1 = new MatchGradeResult(),
            B_2 = new MatchGradeResult(),
            C_1 = new MatchGradeResult(),
            C_2 = new MatchGradeResult(),
            DQB1_1 = new MatchGradeResult(),
            DQB1_2 = new MatchGradeResult(),
            DRB1_1 = new MatchGradeResult(),
            DRB1_2 = new MatchGradeResult(),
        };

        [SetUp]
        public void SetUp()
        {
            scoringLookupService = Substitute.For<IHlaScoringLookupService>();
            gradingService = Substitute.For<IGradingService>();
            confidenceService = Substitute.For<IConfidenceService>();
            rankingService = Substitute.For<IRankingService>();
            matchScoreCalculator = Substitute.For<IMatchScoreCalculator>();

            rankingService.RankSearchResults(Arg.Any<IEnumerable<MatchAndScoreResult>>())
                .Returns(callInfo => (IEnumerable<MatchAndScoreResult>) callInfo.Args().First());
            gradingService.CalculateGrades(null, null).ReturnsForAnyArgs(defaultMatchGradeResults);
            confidenceService.CalculateMatchConfidences(null, null, null).ReturnsForAnyArgs(new PhenotypeInfo<MatchConfidence>());

            donorScoringService =
                new DonorScoringService(scoringLookupService, gradingService, confidenceService, rankingService, matchScoreCalculator);
        }

        [Test]
        public async Task Score_FetchesScoringDataForAllLociForAllResults()
        {
            // 5 loci x 2 x 2 results. This will need updating when DPB1 included
            const int expectedNumberOfFetches = 20;

            var patientHla = new PhenotypeInfo<string>();
            var result1 = new MatchResultBuilder().Build();
            var result2 = new MatchResultBuilder().Build();

            await donorScoringService.Score(patientHla, new[] {result1, result2});

            await scoringLookupService.Received(expectedNumberOfFetches).GetHlaLookupResult(Arg.Any<MatchLocus>(), Arg.Any<string>());
        }

        [Test]
        public async Task Score_DoesNotFetchScoringDataForUntypedLociForResults()
        {
            const Locus locus = Locus.B;
            const string patientHlaAtLocus = "patient-hla-locus-B";
            var patientHla = new PhenotypeInfo<string>();
            patientHla.SetAtLocus(locus, TypePositions.Both, patientHlaAtLocus);
            var result1 = new MatchResultBuilder()
                .WithHlaAtLocus(locus, null)
                .Build();

            await donorScoringService.Score(patientHla, new[] {result1});

            await scoringLookupService.DidNotReceive().GetHlaLookupResult(locus.ToMatchLocus(), Arg.Is<string>(s => s != patientHlaAtLocus));
        }

        [Test]
        public async Task Score_FetchesScoringDataForAllLociForPatientHla()
        {
            // 5 loci x 2 positions. This will need updating when DPB1 included
            const int expectedNumberOfFetches = 10;

            var patientHla = new PhenotypeInfo<string>
            {
                A_1 = "hla",
                A_2 = "hla",
                B_1 = "hla",
                B_2 = "hla",
                C_1 = "hla",
                C_2 = "hla",
                DRB1_1 = "hla",
                DRB1_2 = "hla",
                DQB1_1 = "hla",
                DQB1_2 = "hla",
            };

            await donorScoringService.Score(patientHla, new List<MatchResult>());

            await scoringLookupService.Received(expectedNumberOfFetches).GetHlaLookupResult(Arg.Any<MatchLocus>(), Arg.Any<string>());
        }

        [Test]
        public async Task Score_DoesNotFetchScoringDataForUntypedLociForPatient()
        {
            var patientHla = new PhenotypeInfo<string>
            {
                A_1 = "hla",
                A_2 = "hla",
            };

            await donorScoringService.Score(patientHla, new List<MatchResult>());

            await scoringLookupService.DidNotReceive().GetHlaLookupResult(Locus.B.ToMatchLocus(), Arg.Any<string>());
        }

        [Test]
        public async Task Score_DoesNotModifyMatchDetailsForResults()
        {
            var matchResult = new MatchResultBuilder().Build();

            var results = await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult});

            results.First().MatchResult.ShouldBeEquivalentTo(matchResult);
        }

        [Test]
        public async Task Score_ReturnsMatchGradeForMatchResults()
        {
            const MatchGrade expectedMatchGrade = MatchGrade.GGroup;

            var matchResult = new MatchResultBuilder().Build();

            var matchGrades = defaultMatchGradeResults;
            matchGrades.A_1 = new MatchGradeResult {GradeResult = expectedMatchGrade};
            matchGrades.B_2 = new MatchGradeResult {GradeResult = expectedMatchGrade};

            gradingService.CalculateGrades(null, null)
                .ReturnsForAnyArgs(matchGrades);

            var results = (await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult})).ToList();

            results.First().ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchGrade.Should().Be(expectedMatchGrade);
            results.First().ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchGrade.Should().Be(expectedMatchGrade);
        }

        [Test]
        public async Task Score_CalculatesMatchGradeForEachMatchResult()
        {
            var matchResult1 = new MatchResultBuilder().Build();
            var matchResult2 = new MatchResultBuilder().Build();

            await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult1, matchResult2});

            gradingService.ReceivedWithAnyArgs(2).CalculateGrades(null, null);
        }

        [Test]
        public async Task Score_ReturnsMatchGradeScoreForMatchResults()
        {
            const int expectedMatchGradeScore = 190;

            var matchResult = new MatchResultBuilder().Build();
            matchScoreCalculator.CalculateScoreForMatchGrade(Arg.Any<MatchGrade>()).Returns(expectedMatchGradeScore);
            
            var results = (await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult})).ToList();

            results.First().ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchGradeScore.Should().Be(expectedMatchGradeScore);
            results.First().ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchGradeScore.Should().Be(expectedMatchGradeScore);
        }

        [Test]
        public async Task Score_ReturnsMatchConfidenceForMatchResults()
        {
            const MatchConfidence expectedMatchConfidence = MatchConfidence.Mismatch;

            var matchResult = new MatchResultBuilder().Build();

            confidenceService.CalculateMatchConfidences(null, null, null)
                .ReturnsForAnyArgs(new PhenotypeInfo<MatchConfidence>
                {
                    A_1 = expectedMatchConfidence,
                    B_2 = expectedMatchConfidence,
                });

            var results = (await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult})).ToList();

            results.First().ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchConfidence.Should().Be(expectedMatchConfidence);
            results.First().ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchConfidence.Should().Be(expectedMatchConfidence);
        }

        [Test]
        public async Task Score_ReturnsMatchConfidenceScoreForMatchResults()
        {
            const int expectedMatchConfidenceScore = 7;

            var matchResult = new MatchResultBuilder().Build();
            matchScoreCalculator.CalculateScoreForMatchConfidence(Arg.Any<MatchConfidence>()).Returns(expectedMatchConfidenceScore);
            
            var results = (await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult})).ToList();

            results.First().ScoreResult.ScoreDetailsAtLocusA.ScoreDetailsAtPosition1.MatchConfidenceScore.Should().Be(expectedMatchConfidenceScore);
            results.First().ScoreResult.ScoreDetailsAtLocusB.ScoreDetailsAtPosition2.MatchConfidenceScore.Should().Be(expectedMatchConfidenceScore);
        }

        [Test]
        public async Task Score_CalculatesMatchConfidenceForEachMatchResult()
        {
            var matchResult1 = new MatchResultBuilder().Build();
            var matchResult2 = new MatchResultBuilder().Build();

            await donorScoringService.Score(new PhenotypeInfo<string>(), new[] {matchResult1, matchResult2});

            confidenceService.ReceivedWithAnyArgs(2).CalculateMatchConfidences(null, null, null);
        }

        [Test]
        public async Task Score_RanksResults()
        {
            var expectedSortedResults = new List<MatchAndScoreResult> {new MatchAndScoreResultBuilder().Build()};
            rankingService.RankSearchResults(null).ReturnsForAnyArgs(expectedSortedResults);

            var results = await donorScoringService.Score(new PhenotypeInfo<string>(), new List<MatchResult>());

            results.Should().BeEquivalentTo(expectedSortedResults);
        }
    }
}