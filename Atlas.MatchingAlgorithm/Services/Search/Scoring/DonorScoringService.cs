using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstPatientHla(MatchResultsScoringRequest request);
        Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(DonorHlaScoringRequest request);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IGradingService gradingService;
        private readonly IConfidenceService confidenceService;
        private readonly IRankingService rankingService;
        private readonly IMatchScoreCalculator matchScoreCalculator;
        private readonly IScoreResultAggregator scoreResultAggregator;

        public DonorScoringService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IRankingService rankingService,
            IMatchScoreCalculator matchScoreCalculator,
            IScoreResultAggregator scoreResultAggregator)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.gradingService = gradingService;
            this.confidenceService = confidenceService;
            this.rankingService = rankingService;
            this.matchScoreCalculator = matchScoreCalculator;
            this.scoreResultAggregator = scoreResultAggregator;
        }

        public async Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstPatientHla(MatchResultsScoringRequest request)
        {
            if (request.ScoringCriteria.LociToScore.IsNullOrEmpty())
            {
                return request.MatchResults.Select(m => new MatchAndScoreResult { MatchResult = m });
            }

            var patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);

            var matchAndScoreResults = new List<MatchAndScoreResult>();
            foreach (var matchResult in request.MatchResults)
            {
                matchAndScoreResults.Add(new MatchAndScoreResult
                {
                    MatchResult = matchResult,
                    ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request, patientScoringMetadata)
                });
            }

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        public async Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(DonorHlaScoringRequest request)
        {
            if (request.ScoringCriteria.LociToScore.IsNullOrEmpty())
            {
                return default;
            }

            var patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);
            return await ScoreDonorHlaAgainstPatientMetadata(request.DonorHla.ToPhenotypeInfo(), request, patientScoringMetadata);
        }

        private async Task<ScoreResult> ScoreDonorHlaAgainstPatientMetadata(
            PhenotypeInfo<string> donorHla,
            ScoringRequest request,
            PhenotypeInfo<IHlaScoringMetadata> patientScoringMetadata)
        {
            var donorScoringMetadata = await GetHlaScoringMetadata(donorHla, request.ScoringCriteria.LociToScore);

            var grades = gradingService.CalculateGrades(patientScoringMetadata, donorScoringMetadata);
            var confidences = confidenceService.CalculateMatchConfidences(patientScoringMetadata, donorScoringMetadata, grades);

            var donorScoringInfo = new DonorScoringInfo
            {
                Grades = grades,
                Confidences = confidences,
                DonorHla = donorHla
            };

            return BuildScoreResult(request.ScoringCriteria, donorScoringInfo);
        }

        private async Task<PhenotypeInfo<IHlaScoringMetadata>> GetHlaScoringMetadata(PhenotypeInfo<string> hlaNames, IEnumerable<Locus> lociToScore)
        {
            return await hlaNames.MapAsync(
                async (locus, position, hla) =>
                {
                    // do not perform lookup for an untyped locus or a locus that is not to be scored
                    if (hla == null || !lociToScore.Contains(locus))
                    {
                        return default;
                    }

                    return await hlaMetadataDictionary.GetHlaScoringMetadata(locus, hla);
                });
        }

        private ScoreResult BuildScoreResult(ScoringCriteria criteria, DonorScoringInfo donorScoringInfo)
        {
            var scoreResult = new ScoreResult();

            foreach (var locus in criteria.LociToScore)
            {
                var gradeResultAtPosition1 = donorScoringInfo.Grades.GetPosition(locus, LocusPosition.One).GradeResult;
                var confidenceAtPosition1 = donorScoringInfo.Confidences.GetPosition(locus, LocusPosition.One);
                var gradeResultAtPosition2 = donorScoringInfo.Grades.GetPosition(locus, LocusPosition.Two).GradeResult;
                var confidenceAtPosition2 = donorScoringInfo.Confidences.GetPosition(locus, LocusPosition.Two);

                var scoreDetails = new LocusScoreDetails
                {
                    IsLocusTyped = donorScoringInfo.DonorHla.GetLocus(locus).Position1And2NotNull(),
                    ScoreDetailsAtPosition1 = BuildScoreDetailsForPosition(gradeResultAtPosition1, confidenceAtPosition1),
                    ScoreDetailsAtPosition2 = BuildScoreDetailsForPosition(gradeResultAtPosition2, confidenceAtPosition2)
                };
                scoreResult.SetScoreDetailsForLocus(locus, scoreDetails);
            }

            scoreResult.AggregateScoreDetails = scoreResultAggregator.AggregateScoreDetails(
                new ScoreResultAggregatorParameters
                {
                    ScoreResult = scoreResult,
                    ScoredLoci = criteria.LociToScore.ToList(),
                    LociToExclude = criteria.LociToExcludeFromAggregateScore.ToList()
                });

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

        private class DonorScoringInfo
        {
            public PhenotypeInfo<MatchGradeResult> Grades { get; set; }
            public PhenotypeInfo<MatchConfidence> Confidences { get; set; }
            public PhenotypeInfo<string> DonorHla { get; set; }
        }
    }

    public class MatchResultsScoringRequest : ScoringRequest
    {
        public IEnumerable<MatchResult> MatchResults { get; set; }
    }
}