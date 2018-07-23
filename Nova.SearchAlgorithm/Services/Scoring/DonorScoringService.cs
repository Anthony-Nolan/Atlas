using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> Score(PhenotypeInfo<string> patientHla, IEnumerable<MatchResult> matchResults);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IGradingService gradingService;
        private readonly IConfidenceService confidenceService;
        private readonly IRankingService rankingService;
        private readonly IMatchScoreCalculator matchScoreCalculator;

        public DonorScoringService(
            IHlaScoringLookupService hlaScoringLookupService,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IRankingService rankingService,
            IMatchScoreCalculator matchScoreCalculator
        )
        {
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.gradingService = gradingService;
            this.confidenceService = confidenceService;
            this.rankingService = rankingService;
            this.matchScoreCalculator = matchScoreCalculator;
        }

        public async Task<IEnumerable<MatchAndScoreResult>> Score(PhenotypeInfo<string> patientHla, IEnumerable<MatchResult> matchResults)
        {
            var patientScoringLookupResult = await patientHla.MapAsync(async (locus, pos, hla) => await GetHlaScoringResultsForLocus(locus, hla));

            var donorMatchingResultsAndLookupResults = await Task.WhenAll(matchResults.Select(GetHlaScoringResults));

            var matchAndScoreResults = donorMatchingResultsAndLookupResults
                .Select(donorScoringLookupResult => CombineMatchAndScoreResults(donorScoringLookupResult, patientScoringLookupResult))
                .ToList();

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        private MatchAndScoreResult CombineMatchAndScoreResults(
            Tuple<MatchResult, PhenotypeInfo<IHlaScoringLookupResult>> donorScoringLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> patientScoringLookupResult)
        {
            var matchResult = donorScoringLookupResult.Item1;
            var scoringLookupResult = donorScoringLookupResult.Item2;

            var grades = gradingService.CalculateGrades(patientScoringLookupResult, scoringLookupResult);
            var confidences = confidenceService.CalculateMatchConfidences(patientScoringLookupResult, scoringLookupResult, grades);

            var scoreResult = new ScoreResult();
            var allLoci = LocusHelpers.AllLoci().Except(new[] {Locus.Dpb1});

            foreach (var locus in allLoci)
            {
                var gradeResultAtPosition1 = grades.DataAtPosition(locus, TypePositions.One).GradeResult;
                var confidenceAtPosition1 = confidences.DataAtPosition(locus, TypePositions.One);
                var gradeResultAtPosition2 = grades.DataAtPosition(locus, TypePositions.Two).GradeResult;
                var confidenceAtPosition2 = confidences.DataAtPosition(locus, TypePositions.Two);

                var scoreDetails = BuildLocusScoreDetails(
                    new Tuple<MatchGrade, MatchGrade>(gradeResultAtPosition1, gradeResultAtPosition2),
                    new Tuple<MatchConfidence, MatchConfidence>(confidenceAtPosition1, confidenceAtPosition2
                    ));
                scoreResult.SetScoreDetailsForLocus(locus, scoreDetails);
            }

            return new MatchAndScoreResult
            {
                MatchResult = matchResult,
                ScoreResult = scoreResult
            };
        }

        private LocusScoreDetails BuildLocusScoreDetails(
            Tuple<MatchGrade, MatchGrade> matchGrades,
            Tuple<MatchConfidence, MatchConfidence> matchConfidences)
        {
            var scoreDetails = new LocusScoreDetails
            {
                ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                {
                    MatchGrade = matchGrades.Item1,
                    MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(matchGrades.Item1),
                    MatchConfidence = matchConfidences.Item1,
                    MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(matchConfidences.Item1),
                },
                ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                {
                    MatchGrade = matchGrades.Item2,
                    MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(matchGrades.Item2),
                    MatchConfidence = matchConfidences.Item2,
                    MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(matchConfidences.Item2),
                }
            };
            return scoreDetails;
        }

        private async Task<Tuple<MatchResult, PhenotypeInfo<IHlaScoringLookupResult>>> GetHlaScoringResults(MatchResult matchResult)
        {
            var scoringLookupResult = await matchResult.Donor.HlaNames.MapAsync(
                async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla)
            );
            return new Tuple<MatchResult, PhenotypeInfo<IHlaScoringLookupResult>>(matchResult, scoringLookupResult);
        }

        private async Task<IHlaScoringLookupResult> GetHlaScoringResultsForLocus(Locus locus, string hla)
        {
            // TODO: NOVA-1301: Implement DPB1 scoring
            if (locus == Locus.Dpb1 || hla == null)
            {
                return null;
            }

            return await hlaScoringLookupService.GetHlaLookupResult(locus.ToMatchLocus(), hla);
        }
    }
}