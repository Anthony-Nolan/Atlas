using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Scoring;
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

        private DonorScoringService donorScoringService;

        [SetUp]
        public void SetUp()
        {
            scoringLookupService = Substitute.For<IHlaScoringLookupService>();
            gradingService = Substitute.For<IGradingService>();
            confidenceService = Substitute.For<IConfidenceService>();
            rankingService = Substitute.For<IRankingService>();

            donorScoringService = new DonorScoringService(scoringLookupService, gradingService, confidenceService, rankingService);
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

            await scoringLookupService.Received(expectedNumberOfFetches).GetHlaScoringLookupResults(Arg.Any<MatchLocus>(), Arg.Any<string>());
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

            await scoringLookupService.DidNotReceive().GetHlaScoringLookupResults(locus.ToMatchLocus(), Arg.Is<string>(s => s != patientHlaAtLocus));
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

            await scoringLookupService.Received(expectedNumberOfFetches).GetHlaScoringLookupResults(Arg.Any<MatchLocus>(), Arg.Any<string>());
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

            await scoringLookupService.DidNotReceive().GetHlaScoringLookupResults(Locus.B.ToMatchLocus(), Arg.Any<string>());
        }
    }
}