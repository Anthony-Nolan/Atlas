using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
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
            var patientScoringLookupResult =
                await patientHla.MapAsync(async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla));

            var donorMatchingResultsAndLookupResults = await Task.WhenAll(matchResults
                .Select(async matchResult =>
                {
                    var scoringLookupResult = await matchResult.Donor.HlaNames.MapAsync(async (locus, position, hla) =>
                        await GetHlaScoringResultsForLocus(locus, hla));
                    return new Tuple<MatchResult, PhenotypeInfo<IHlaScoringLookupResult>>(matchResult, scoringLookupResult);
                }));

            var matchAndScoreResults = donorMatchingResultsAndLookupResults.Select(donorScoringLookupResult =>
            {
                var matchResult = donorScoringLookupResult.Item1;
                var scoringLookupResult = donorScoringLookupResult.Item2;

                var grades = gradingService.CalculateGrades(patientScoringLookupResult, scoringLookupResult);
                var confidences = confidenceService.CalculateMatchConfidences(patientScoringLookupResult, scoringLookupResult, grades);

                var scoreResult = new ScoreResult();
                var allLoci = LocusHelpers.AllLoci().Except(new[] {Locus.Dpb1});

                foreach (var locus in allLoci)
                {
                    var gradeResultAtPositionOne = grades.DataAtPosition(locus, TypePositions.One).GradeResult;
                    var confidenceAtPositionOne = confidences.DataAtPosition(locus, TypePositions.One);
                    var gradeResultAtPositionTwo = grades.DataAtPosition(locus, TypePositions.Two).GradeResult;
                    var confidenceAtPositionTwo = confidences.DataAtPosition(locus, TypePositions.Two);
                    
                    var scoreDetails = new LocusScoreDetails
                    {
                        ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                        {
                            MatchGrade = gradeResultAtPositionOne,
                            MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(gradeResultAtPositionOne),
                            MatchConfidence = confidenceAtPositionOne,
                            MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(confidenceAtPositionOne),
                        },
                        ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                        {
                            MatchGrade = gradeResultAtPositionTwo,
                            MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(gradeResultAtPositionTwo),
                            MatchConfidence = confidenceAtPositionTwo,
                            MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(confidenceAtPositionTwo),
                        }
                    };
                    scoreResult.SetScoreDetailsForLocus(locus, scoreDetails);
                }

                return new MatchAndScoreResult
                {
                    MatchResult = matchResult,
                    ScoreResult = scoreResult
                };
            }).ToList();

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        private async Task<IHlaScoringLookupResult> GetHlaScoringResultsForLocus(Locus locus, string hla)
        {
            if (locus == Locus.Dpb1 || hla == null)
            {
                return null;
            }

            return await hlaScoringLookupService.GetHlaLookupResult(locus.ToMatchLocus(), hla);
        }
    }
}