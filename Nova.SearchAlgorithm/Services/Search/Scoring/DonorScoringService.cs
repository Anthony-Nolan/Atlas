using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Config;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstHla(IEnumerable<MatchResult> matchResults, PhenotypeInfo<string> patientHla);
        Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(PhenotypeInfo<string> donorHla, PhenotypeInfo<string> patientHla);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IGradingService gradingService;
        private readonly IConfidenceService confidenceService;
        private readonly IRankingService rankingService;
        private readonly IMatchScoreCalculator matchScoreCalculator;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public DonorScoringService(
            IHlaScoringLookupService hlaScoringLookupService,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IRankingService rankingService,
            IMatchScoreCalculator matchScoreCalculator,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider
        )
        {
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.gradingService = gradingService;
            this.confidenceService = confidenceService;
            this.rankingService = rankingService;
            this.matchScoreCalculator = matchScoreCalculator;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public async Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstHla(
            IEnumerable<MatchResult> matchResults,
            PhenotypeInfo<string> patientHla)
        {
            var patientScoringLookupResult = await GetHlaScoringResults(patientHla);

            var matchAndScoreResults = await Task.WhenAll(matchResults
                .AsParallel()
                .Select(async matchResult =>
                {
                    var lookupResult = await GetHlaScoringResults(matchResult.Donor.HlaNames);
                    var scoreResult = ScoreDonorAndPatient(lookupResult, patientScoringLookupResult);
                    return CombineMatchAndScoreResults(matchResult, scoreResult);
                })
                .ToList()
            );

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        public async Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(PhenotypeInfo<string> donorHla, PhenotypeInfo<string> patientHla)
        {
            var patientLookupResult = await GetHlaScoringResults(patientHla);
            var donorLookupResult = await GetHlaScoringResults(donorHla);
            return ScoreDonorAndPatient(donorLookupResult, patientLookupResult);
        }

        private static MatchAndScoreResult CombineMatchAndScoreResults(MatchResult matchResult, ScoreResult scoreResult)
        {
            return new MatchAndScoreResult
            {
                MatchResult = matchResult,
                ScoreResult = scoreResult
            };
        }

        private ScoreResult ScoreDonorAndPatient(
            PhenotypeInfo<IHlaScoringLookupResult> donorScoringLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> patientScoringLookupResult
        )
        {
            var grades = gradingService.CalculateGrades(patientScoringLookupResult, donorScoringLookupResult);
            var confidences = confidenceService.CalculateMatchConfidences(patientScoringLookupResult, donorScoringLookupResult, grades);

            var locusTypingInformation = donorScoringLookupResult.Map((l, p, result) => result != null);

            var scoreResult = BuildScoreResult(grades, confidences, locusTypingInformation);
            return scoreResult;
        }

        private ScoreResult BuildScoreResult(
            PhenotypeInfo<MatchGradeResult> grades,
            PhenotypeInfo<MatchConfidence> confidences,
            PhenotypeInfo<bool> locusTypingInformation
        )
        {
            var scoreResult = new ScoreResult();
            var scoredLoci = LocusSettings.AllLoci;

            foreach (var locus in scoredLoci)
            {
                var gradeResultAtPosition1 = grades.DataAtPosition(locus, TypePosition.One).GradeResult;
                var confidenceAtPosition1 = confidences.DataAtPosition(locus, TypePosition.One);
                var gradeResultAtPosition2 = grades.DataAtPosition(locus, TypePosition.Two).GradeResult;
                var confidenceAtPosition2 = confidences.DataAtPosition(locus, TypePosition.Two);

                var scoreDetails = new LocusScoreDetails
                {
                    // Either position can be used here, as the locus will either be typed at both positions or neither
                    IsLocusTyped = locusTypingInformation.DataAtPosition(locus, TypePosition.One),
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

        private async Task<PhenotypeInfo<IHlaScoringLookupResult>> GetHlaScoringResults(PhenotypeInfo<string> hlaNames)
        {
            return await hlaNames.MapAsync(
                async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla)
            );
        }

        private async Task<IHlaScoringLookupResult> GetHlaScoringResultsForLocus(Locus locus, string hla)
        {
            return hla != null 
                ? await hlaScoringLookupService.GetHlaLookupResult(locus, hla, wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion())
                : null;
        }
    }
}