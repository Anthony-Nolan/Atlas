using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchService
    {
        Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest);
    }

    public class SearchService : ISearchService
    {
        private const string LoggingPrefix = "Matching Algorithm: ";

        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IDonorScoringService donorScoringService;
        private readonly IMatchingService matchingService;
        private readonly ILogger searchLogger;

        public SearchService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IDonorScoringService donorScoringService,
            IMatchingService matchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger
        )
        {
            this.donorScoringService = donorScoringService;
            this.matchingService = matchingService;
            this.searchLogger = searchLogger;
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public async Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest)
        {
            var expansionTimer = searchLogger.RunTimed($"{LoggingPrefix}Expand patient HLA");
            var criteria = await GetMatchCriteria(matchingRequest);
            expansionTimer.Dispose();

            var matchingTimer = searchLogger.RunTimed($"{LoggingPrefix}Matching");
            var matches = (await matchingService.GetMatches(criteria)).ToList();
            matchingTimer.Dispose();

            searchLogger.SendTrace($"{LoggingPrefix}Matched {matches.Count} donors.");
            
            var scoringTimer = searchLogger.RunTimed($"{LoggingPrefix}Scoring");
            var request = new MatchResultsScoringRequest
            {
                PatientHla = matchingRequest.SearchHlaData,
                MatchResults = matches,
                ScoringCriteria = matchingRequest.ScoringCriteria
            };
            var scoredMatches= await donorScoringService.ScoreMatchesAgainstPatientHla(request);
            scoringTimer.Dispose();

            return scoredMatches.Select(MapSearchResultToApiSearchResult);
        }

        private async Task<AlleleLevelMatchCriteria> GetMatchCriteria(SearchRequest matchingRequest)
        {
            var matchCriteria = matchingRequest.MatchCriteria;
            var searchHla = matchingRequest.SearchHlaData.ToPhenotypeInfo();
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchCriteria.A, searchHla.A),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchCriteria.B, searchHla.B),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchCriteria.C, searchHla.C),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchCriteria.Drb1, searchHla.Drb1),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchCriteria.Dqb1, searchHla.Dqb1));

            return new AlleleLevelMatchCriteria
            {
                SearchType = matchingRequest.SearchDonorType.ToMatchingAlgorithmDonorType(),
                DonorMismatchCount = matchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDrb1 = criteriaMappings[3],
                LocusMismatchDqb1 = criteriaMappings[4]
            };
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus,
            int? allowedMismatches,
            LocusInfo<string> searchHla)
        {
            if (allowedMismatches == null)
            {
                return null;
            }

            var searchTerm = new LocusInfo<string>(searchHla.Position1, searchHla.Position2);

            var metadata = await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(
                locus,
                searchTerm
            );

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = allowedMismatches.Value,
                PGroupsToMatchInPositionOne = metadata.Position1.MatchingPGroups,
                PGroupsToMatchInPositionTwo = metadata.Position2.MatchingPGroups
            };
        }

        private static MatchingAlgorithmResult MapSearchResultToApiSearchResult(MatchAndScoreResult result)
        {
            return new MatchingAlgorithmResult
            {
                AtlasDonorId = result.MatchResult.DonorInfo.DonorId,
                DonorType = result.MatchResult.DonorInfo.DonorType.ToAtlasClientModel(),
                
                MatchingResult = new MatchingResult
                {
                    TotalMatchCount = result.MatchResult.TotalMatchCount,
                    DonorHla = result.MatchResult.DonorInfo.HlaNames.ToPhenotypeInfoTransfer(),
                    TypedLociCount = result.MatchResult.TypedLociCount,
                },

                ScoringResult = new ScoringResult
                {
                    MatchCategory = result.ScoreResult?.AggregateScoreDetails.MatchCategory,
                    ConfidenceScore = result.ScoreResult?.AggregateScoreDetails.ConfidenceScore,
                    GradeScore = result.ScoreResult?.AggregateScoreDetails.GradeScore,
                    TypedLociCountAtScoredLoci = result.ScoreResult?.AggregateScoreDetails.TypedLociCount,
                    PotentialMatchCount = result.PotentialMatchCount,
                    
                    ScoringResultsByLocus = new LociInfo<LocusSearchResult>().Map((l, _) => MapSearchResultToApiLocusSearchResult(result, l)).ToLociInfoTransfer(),
                },
            };
        }

        private static LocusSearchResult MapSearchResultToApiLocusSearchResult(MatchAndScoreResult result, Locus locus)
        {
            var matchDetailsForLocus = result.MatchResult.MatchDetailsForLocus(locus);
            var scoreDetailsForLocus = result.ScoreResult?.ScoreDetailsForLocus(locus);

            // do not return a result if neither matching nor scoring was performed at this locus
            if (matchDetailsForLocus == null && scoreDetailsForLocus == null)
            {
                return default;
            }

            return new LocusSearchResult
            {
                MatchCount = matchDetailsForLocus?.MatchCount ?? scoreDetailsForLocus.MatchCount(),

                IsLocusMatchCountIncludedInTotal = matchDetailsForLocus != null,

                // scoring results
                IsLocusTyped = scoreDetailsForLocus?.IsLocusTyped,
                MatchGradeScore = scoreDetailsForLocus?.MatchGradeScore,
                MatchConfidenceScore = scoreDetailsForLocus?.MatchConfidenceScore,
                ScoreDetailsAtPositionOne = scoreDetailsForLocus?.ScoreDetailsAtPosition1,
                ScoreDetailsAtPositionTwo = scoreDetailsForLocus?.ScoreDetailsAtPosition2
            };
        }
    }
}