using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IDonorScoringService
    {
        Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(DonorHlaScoringRequest request);

        Task<Dictionary<PhenotypeInfo<string>, ScoreResult>> ScoreDonorsHlaAgainstPatientHla(
            List<PhenotypeInfo<string>> distinctDonorPhenotypes,
            PhenotypeInfo<string> patientPhenotypeInfo,
            ScoringCriteria scoringCriteria);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IGradingService gradingService;
        private readonly IConfidenceService confidenceService;
        private readonly IAntigenMatchingService antigenMatchingService;
        private readonly IMatchScoreCalculator matchScoreCalculator;
        private readonly IScoreResultAggregator scoreResultAggregator;
        private readonly IDpb1TceGroupMatchCalculator dpb1TceGroupMatchCalculator;
        private readonly ILogger logger;

        public DonorScoringService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IAntigenMatchingService antigenMatchingService,
            IMatchScoreCalculator matchScoreCalculator,
            IScoreResultAggregator scoreResultAggregator,
            IDpb1TceGroupMatchCalculator dpb1TceGroupMatchCalculator,
            ILogger logger)
        {
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.gradingService = gradingService;
            this.confidenceService = confidenceService;
            this.antigenMatchingService = antigenMatchingService;
            this.matchScoreCalculator = matchScoreCalculator;
            this.scoreResultAggregator = scoreResultAggregator;
            this.dpb1TceGroupMatchCalculator = dpb1TceGroupMatchCalculator;
            this.logger = logger;
        }

        public async Task<ScoreResult> ScoreDonorHlaAgainstPatientHla(DonorHlaScoringRequest request)
        {
            if (request.ScoringCriteria.LociToScore.IsNullOrEmpty())
            {
                return default;
            }

            var patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);
            return await ScoreDonorHlaAgainstPatientMetadata(request.DonorHla.ToPhenotypeInfo(), request.ScoringCriteria, patientScoringMetadata);
        }

        public async Task<Dictionary<PhenotypeInfo<string>, ScoreResult>> ScoreDonorsHlaAgainstPatientHla(
            List<PhenotypeInfo<string>> donorsHla,
            PhenotypeInfo<string> patientHla,
            ScoringCriteria scoringCriteria)
        {
            var patientMetadata = await GetHlaScoringMetadata(patientHla, scoringCriteria.LociToScore);
            logger.SendTrace($"Received patient scoring HLA result", LogLevel.Info);

            var scoringResultsPerDonorsHla = new Dictionary<PhenotypeInfo<string>, ScoreResult>();

            // we are deliberately avoiding running scoring for multiple donors in parallel for now to minimize load on scoring system.
            // in case we find performance issues with this approach, it'll be changed later.
            for (var i = 0; i < donorsHla.Count; i++)
            {
                var donorHla = donorsHla[i];
                ScoreResult scoringResult = null;
                try
                {
                    scoringResult = await ScoreDonorHlaAgainstPatientMetadata(donorHla, scoringCriteria, patientMetadata);
                    logger.SendTrace($"Received scoring result for donor {i + 1} / {donorsHla.Count}", LogLevel.Verbose);
                }
                catch (Exception ex)
                {
                    logger.SendTrace($"Could not get score result for one of the donors. Exception: {ex}", LogLevel.Error);
                }

                scoringResultsPerDonorsHla[donorHla] = scoringResult;
            }

            logger.SendTrace($"Received scoring results for {donorsHla.Count} donors", LogLevel.Info);

            return scoringResultsPerDonorsHla;
        }

        protected async Task<ScoreResult> ScoreDonorHlaAgainstPatientMetadata(
            PhenotypeInfo<string> donorHla,
            ScoringCriteria scoringCriteria,
            PhenotypeInfo<IHlaScoringMetadata> patientScoringMetadata)
        {
            var donorScoringInfo = await CalculateDonorScoringInfo(donorHla, scoringCriteria, patientScoringMetadata);

            var dpb1TceGroupMatchType = await dpb1TceGroupMatchCalculator.CalculateDpb1TceGroupMatchType(
                patientScoringMetadata.Dpb1.Map(x => x?.LookupName),
                donorHla.Dpb1);

            return BuildScoreResult(scoringCriteria, donorScoringInfo, dpb1TceGroupMatchType);
        }

        private async Task<PhenotypeInfo<DonorScoringInfo>> CalculateDonorScoringInfo(
            PhenotypeInfo<string> donorHla,
            ScoringCriteria scoringCriteria,
            PhenotypeInfo<IHlaScoringMetadata> patientScoringMetadata)
        {
            var donorScoringMetadata = await GetHlaScoringMetadata(donorHla, scoringCriteria.LociToScore);

            // Grading should be called first and calculated for both orientations,
            // as all remaining score types should be calculated in the orientations that lead to the best possible match grades.
            var grades = gradingService.Score(
                new LociInfo<IEnumerable<MatchOrientation>>(new[] { MatchOrientation.Direct, MatchOrientation.Cross }),
                patientScoringMetadata,
                donorScoringMetadata);

            var confidences = confidenceService.Score(
                grades.GetMatchOrientations(), patientScoringMetadata, donorScoringMetadata);

            // Confidence service may have further refined the best orientations, so its output is passed in here.
            var antigenMatches = antigenMatchingService.Score(
                confidences.GetMatchOrientations(), patientScoringMetadata, donorScoringMetadata);

            return donorHla.Map((locus, position, hlaName) => scoringCriteria.LociToScore.Contains(locus) ?
                new DonorScoringInfo
                {
                    HlaName = hlaName,
                    Grade = grades.GetLocus(locus).LocusScore.GetAtPosition(position),
                    Confidence = confidences.GetLocus(locus).LocusScore.GetAtPosition(position),
                    IsAntigenMatch = antigenMatches.GetLocus(locus).LocusScore.GetAtPosition(position)
                } : null);
        }

        protected async Task<PhenotypeInfo<IHlaScoringMetadata>> GetHlaScoringMetadata(PhenotypeInfo<string> hlaNames, IEnumerable<Locus> lociToScore)
        {
            return await hlaNames.MapAsync(
                async (locus, _, hla) =>
                {
                    // do not perform lookup for an untyped locus or a locus that is not to be scored
                    if (hla.IsNullOrEmpty() || !lociToScore.Contains(locus))
                    {
                        return default;
                    }

                    return await hlaMetadataDictionary.GetHlaScoringMetadata(locus, hla);
                });
        }

        private ScoreResult BuildScoreResult(ScoringCriteria criteria, PhenotypeInfo<DonorScoringInfo> donorScoringInfo, Dpb1TceGroupMatchType dpb1TceGroupMatchType)
        {
            var scoreResult = new ScoreResult();

            foreach (var locus in criteria.LociToScore)
            {
                var scoreDetailsPerPosition = new LocusInfo<LocusPositionScoreDetails>(p =>
                    BuildScoreDetailsForPosition(donorScoringInfo.GetPosition(locus, p)));

                var matchCategory = locus == Locus.Dpb1
                    ? LocusMatchCategoryAggregator.Dpb1MatchCategoryFromPositionScores(scoreDetailsPerPosition, dpb1TceGroupMatchType)
                    : LocusMatchCategoryAggregator.LocusMatchCategoryFromPositionScores(scoreDetailsPerPosition);

                var scoreDetails = new LocusScoreDetails
                {
                    IsLocusTyped = donorScoringInfo.GetLocus(locus).Map(x => x.HlaName).Position1And2NotNull(),
                    ScoreDetailsAtPosition1 = scoreDetailsPerPosition.Position1,
                    ScoreDetailsAtPosition2 = scoreDetailsPerPosition.Position2,
                    MatchCategory = matchCategory,
                    MismatchDirection = GetMismatchDirection(matchCategory, locus, dpb1TceGroupMatchType)
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

        private LocusPositionScoreDetails BuildScoreDetailsForPosition(DonorScoringInfo scoringInfo)
        {
            return new LocusPositionScoreDetails
            {
                MatchGrade = scoringInfo.Grade,
                MatchGradeScore = matchScoreCalculator.CalculateScoreForMatchGrade(scoringInfo.Grade),
                MatchConfidence = scoringInfo.Confidence,
                MatchConfidenceScore = matchScoreCalculator.CalculateScoreForMatchConfidence(scoringInfo.Confidence),
                IsAntigenMatch = scoringInfo.IsAntigenMatch
            };
        }

        private static MismatchDirection? GetMismatchDirection(
            LocusMatchCategory locusMatchCategory,
            Locus locus,
            Dpb1TceGroupMatchType dpb1TceGroupMatchType)
        {
            if (locus != Locus.Dpb1)
            {
                return null;
            }

            if (locusMatchCategory != LocusMatchCategory.Mismatch)
            {
                return MismatchDirection.NotApplicable;
            }

            return LocusMatchCategoryAggregator.GetMismatchDirection(dpb1TceGroupMatchType);
        }

        private class DonorScoringInfo
        {
            public string HlaName { get; init; }
            public MatchGrade Grade { get; init; }
            public MatchConfidence Confidence { get; init; }
            public bool? IsAntigenMatch { get; init; }
        }
    }
}