using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstHla(IEnumerable<MatchResult> matchResults, PhenotypeInfo<string> patientHla);
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

        public async Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstHla(
            IEnumerable<MatchResult> matchResults,
            PhenotypeInfo<string> patientHla)
        {
            var patientScoringLookupResult = await patientHla.MapAsync(async (locus, pos, hla) => await GetHlaScoringResultsForLocus(locus, hla));

            var matchAndScoreResults = await Task.WhenAll(matchResults
                .Select(async matchResult =>
                {
                    var lookupResult = await GetHlaScoringResults(matchResult);
                    return CombineMatchAndScoreResults(matchResult, lookupResult, patientScoringLookupResult);
                })
                .ToList()
            );

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        private MatchAndScoreResult CombineMatchAndScoreResults(
            MatchResult matchResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorScoringLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> patientScoringLookupResult)
        {
            var grades = gradingService.CalculateGrades(patientScoringLookupResult, donorScoringLookupResult);
            var confidences = confidenceService.CalculateMatchConfidences(patientScoringLookupResult, donorScoringLookupResult, grades);

            var scoreResult = BuildScoreResult(grades, confidences);

            return new MatchAndScoreResult
            {
                MatchResult = matchResult,
                ScoreResult = scoreResult
            };
        }

        private ScoreResult BuildScoreResult(PhenotypeInfo<MatchGradeResult> grades, PhenotypeInfo<MatchConfidence> confidences)
        {
            var scoreResult = new ScoreResult();

            // TODO: NOVA-1301: Score DPB1
            var scoredLoci = LocusHelpers.AllLoci().Except(new[] {Locus.Dpb1});

            foreach (var locus in scoredLoci)
            {
                var gradeResultAtPosition1 = grades.DataAtPosition(locus, TypePositions.One).GradeResult;
                var confidenceAtPosition1 = confidences.DataAtPosition(locus, TypePositions.One);
                var gradeResultAtPosition2 = grades.DataAtPosition(locus, TypePositions.Two).GradeResult;
                var confidenceAtPosition2 = confidences.DataAtPosition(locus, TypePositions.Two);

                var scoreDetails = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = BuildScoreDetailsForPosition(gradeResultAtPosition1, confidenceAtPosition1),
                    ScoreDetailsAtPosition2 = BuildScoreDetailsForPosition(gradeResultAtPosition2, confidenceAtPosition2)
                };
                scoreResult.SetScoreDetailsForLocus(locus, scoreDetails);
            }

            return scoreResult;
        }

        private LocusPositionScoreDetails BuildScoreDetailsForPosition(MatchGrade matchGrade, MatchConfidence matchConfidence)
        {
            return new LocusPositionScoreDetails
            {
                MatchGrade = matchGrade,
                MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(matchGrade),
                MatchConfidence = matchConfidence,
                MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(matchConfidence),
            };
        }

        private async Task<PhenotypeInfo<IHlaScoringLookupResult>> GetHlaScoringResults(MatchResult matchResult)
        {
            return await matchResult.Donor.HlaNames.MapAsync(
                async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla)
            );
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