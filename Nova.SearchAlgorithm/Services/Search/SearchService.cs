using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Search.Scoring;
using Nova.Utils.ApplicationInsights;
using SearchResult = Nova.SearchAlgorithm.Client.Models.SearchResults.SearchResult;

namespace Nova.SearchAlgorithm.Services.Search
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly ILocusHlaMatchingLookupService locusHlaMatchingLookupService;
        private readonly IDonorScoringService donorScoringService;
        private readonly IDonorMatchingService donorMatchingService;
        private readonly ILogger logger;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public SearchService(
            ILocusHlaMatchingLookupService locusHlaMatchingLookupService,
            IDonorScoringService donorScoringService,
            IDonorMatchingService donorMatchingService,
            ILogger logger,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider
        )
        {
            this.locusHlaMatchingLookupService = locusHlaMatchingLookupService;
            this.donorScoringService = donorScoringService;
            this.donorMatchingService = donorMatchingService;
            this.logger = logger;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public async Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var criteria = await GetMatchCriteria(searchRequest);
            var patientHla = GetPatientHla(searchRequest);

            logger.SendTrace("Search timing: Looked up patient hla", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()}
            });
            stopwatch.Restart();

            var matches = (await donorMatchingService.GetMatches(criteria)).ToList();

            logger.SendTrace("Search timing: Matching complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"MatchedDonors", matches.Count().ToString()}
            });
            stopwatch.Restart();

            var scoredMatches = await donorScoringService.ScoreMatchesAgainstHla(matches, patientHla);

            logger.SendTrace("Search timing: Scoring complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"MatchedDonors", matches.Count().ToString()}
            });

            return scoredMatches.Select(MapSearchResultToApiSearchResult);
        }

        private async Task<AlleleLevelMatchCriteria> GetMatchCriteria(SearchRequest searchRequest)
        {
            var matchCriteria = searchRequest.MatchCriteria;
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchA, searchRequest.SearchHlaData.LocusSearchHlaA),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchB, searchRequest.SearchHlaData.LocusSearchHlaB),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchC, searchRequest.SearchHlaData.LocusSearchHlaC),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchDrb1, searchRequest.SearchHlaData.LocusSearchHlaDrb1),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchDqb1, searchRequest.SearchHlaData.LocusSearchHlaDqb1));

            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCount = (int) matchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDrb1 = criteriaMappings[3],
                LocusMismatchDqb1 = criteriaMappings[4]
            };
            return criteria;
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus,
            LocusMismatchCriteria mismatchCriteria,
            LocusSearchHla searchHla)
        {
            if (mismatchCriteria == null)
            {
                return null;
            }

            var lookupResult = await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(
                locus,
                new Tuple<string, string>(searchHla.SearchHla1, searchHla.SearchHla2),
                wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion()
            );

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatchCriteria.MismatchCount,
                PGroupsToMatchInPositionOne = lookupResult.Item1.MatchingPGroups,
                PGroupsToMatchInPositionTwo = lookupResult.Item2.MatchingPGroups
            };
        }

        private static PhenotypeInfo<string> GetPatientHla(SearchRequest searchRequest)
        {
            return searchRequest.SearchHlaData.ToPhenotypeInfo();
        }

        private static SearchResult MapSearchResultToApiSearchResult(MatchAndScoreResult result)
        {
            return new SearchResult
            {
                DonorId = result.MatchResult.Donor.DonorId,
                DonorType = result.MatchResult.Donor.DonorType,
                Registry = result.MatchResult.Donor.RegistryCode,
                OverallMatchConfidence = result.ScoreResult.OverallMatchConfidence,
                ConfidenceScore = result.ScoreResult.ConfidenceScore,
                GradeScore = result.ScoreResult.GradeScore,
                TotalMatchCount = result.MatchResult.TotalMatchCount,
                PotentialMatchCount = result.PotentialMatchCount,
                TypedLociCount = result.ScoreResult.TypedLociCount,
                SearchResultAtLocusA = MapSearchResultToApiLocusSearchResult(result, Locus.A),
                SearchResultAtLocusB = MapSearchResultToApiLocusSearchResult(result, Locus.B),
                SearchResultAtLocusC = MapSearchResultToApiLocusSearchResult(result, Locus.C),
                SearchResultAtLocusDpb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Dpb1),
                SearchResultAtLocusDqb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Dqb1),
                SearchResultAtLocusDrb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Drb1),
            };
        }

        private static LocusSearchResult MapSearchResultToApiLocusSearchResult(MatchAndScoreResult result, Locus locus)
        {
            var matchDetailsForLocus = result.MatchResult.MatchDetailsForLocus(locus);
            var scoreDetailsForLocus = result.ScoreResult.ScoreDetailsForLocus(locus);

            return new LocusSearchResult
            {
                IsLocusTyped = scoreDetailsForLocus.IsLocusTyped,
                MatchCount = matchDetailsForLocus?.MatchCount ?? scoreDetailsForLocus.MatchCount(),
                IsLocusMatchCountIncludedInTotal = matchDetailsForLocus != null,
                MatchGradeScore = scoreDetailsForLocus.MatchGradeScore,
                MatchConfidenceScore = scoreDetailsForLocus.MatchConfidenceScore,
                ScoreDetailsAtPositionOne = scoreDetailsForLocus.ScoreDetailsAtPosition1,
                ScoreDetailsAtPositionTwo = scoreDetailsForLocus.ScoreDetailsAtPosition2
            };
        }
    }
}