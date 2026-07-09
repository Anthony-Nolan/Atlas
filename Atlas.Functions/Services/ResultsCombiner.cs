using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Services.MatchCategories;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        Task<IEnumerable<SearchResult>> CombineResults(
            string searchRequestId,
            IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults,
            IReadOnlyDictionary<int, string> matchPredictionResultLocations);

        /// <summary>
        /// Combines matching results with already-downloaded match prediction results (keyed by Atlas donor id).
        /// Used by the parallel path.
        /// </summary>
        IEnumerable<SearchResult> CombineResults(
            string searchRequestId,
            IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults,
            IReadOnlyDictionary<int, MatchProbabilityResponse> matchPredictionResults);

        /// <summary>
        /// Downloads every per-batch result blob (each a <c>Dictionary&lt;int, MatchProbabilityResponse&gt;</c>) and
        /// merges them into a single donor → result map.
        /// </summary>
        Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> DownloadBatchedMatchPredictionResults(
            IReadOnlyCollection<string> batchResultLocations);

        SearchResultSet BuildResultsSummary(ResultSet<MatchingAlgorithmResult> matchingAlgorithmResultSet, TimeSpan matchPredictionTime, TimeSpan matchingTime);
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly IAtlasLogger logger;
        private readonly IBlobDownloader blobDownloader;
        private readonly IPositionalMatchCategoryService matchCategoryService;
        private readonly string resultsContainer;
        private readonly string matchPredictionResultsContainer;
        private readonly int matchPredictionDownloadBatchSize;

        public ResultsCombiner(
            IOptions<AzureStorageSettings> azureStorageSettings,
            ISearchLogger<SearchLoggingContext> logger,
            IBlobDownloader blobDownloader,
            IPositionalMatchCategoryService matchCategoryService)
        {
            this.logger = logger;
            this.blobDownloader = blobDownloader;
            this.matchCategoryService = matchCategoryService;
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
            matchPredictionResultsContainer = azureStorageSettings.Value.MatchPredictionResultsBlobContainer;
            matchPredictionDownloadBatchSize = azureStorageSettings.Value.MatchPredictionDownloadBatchSize;
        }

        public SearchResultSet BuildResultsSummary(ResultSet<MatchingAlgorithmResult> matchingAlgorithmResultSet, TimeSpan matchPredictionTime, TimeSpan matchingTime)
        {
            using (logger.RunTimed($"Build results summary: {matchingAlgorithmResultSet.SearchRequestId}"))
            {
                var resultSet = matchingAlgorithmResultSet is RepeatMatchingAlgorithmResultSet repeatSet
                    ? new RepeatSearchResultSet
                    {
                        RepeatSearchId = repeatSet.RepeatSearchId,
                        NoLongerMatchingDonorCodes = repeatSet.NoLongerMatchingDonors
                    } as SearchResultSet
                    : new OriginalSearchResultSet();

                resultSet.TotalResults = matchingAlgorithmResultSet.TotalResults;
                resultSet.MatchingAlgorithmHlaNomenclatureVersion = matchingAlgorithmResultSet.MatchingAlgorithmHlaNomenclatureVersion;
                resultSet.SearchRequestId = matchingAlgorithmResultSet.SearchRequestId;
                resultSet.BlobStorageContainerName = resultsContainer;
                resultSet.MatchingAlgorithmTime = matchingTime;
                resultSet.MatchPredictionTime = matchPredictionTime;
                resultSet.SearchRequest = matchingAlgorithmResultSet.SearchRequest;
                resultSet.MatchingStartTime = matchingAlgorithmResultSet.MatchingStartTime;

                return resultSet;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SearchResult>> CombineResults(
            string searchRequestId,
            IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults,
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed($"Combine search results: {searchRequestId}"))
            {
                var matchPredictionResults = await DownloadMatchPredictionResults(matchPredictionResultLocations);
                return matchingAlgorithmResults.Select(r => new SearchResult
                {
                    DonorCode = r.MatchingDonorInfo.ExternalDonorCode,
                    MatchingResult = r,
                    MatchPredictionResult = matchCategoryService.ReOrientatePositionalMatchCategories(matchPredictionResults[r.AtlasDonorId], r.ScoringResult)
                });
            }
        }

        /// <inheritdoc />
        public IEnumerable<SearchResult> CombineResults(
            string searchRequestId,
            IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults,
            IReadOnlyDictionary<int, MatchProbabilityResponse> matchPredictionResults)
        {
            using (logger.RunTimed($"Combine search results: {searchRequestId}"))
            {
                return matchingAlgorithmResults.Select(r => new SearchResult
                {
                    DonorCode = r.MatchingDonorInfo.ExternalDonorCode,
                    MatchingResult = r,
                    MatchPredictionResult = matchCategoryService.ReOrientatePositionalMatchCategories(matchPredictionResults[r.AtlasDonorId], r.ScoringResult)
                }).ToList();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> DownloadBatchedMatchPredictionResults(
            IReadOnlyCollection<string> batchResultLocations)
        {
            using (logger.RunTimed("Download match prediction algorithm results"))
            {
                logger.SendTrace($"{batchResultLocations.Count} batch result file(s) to download");

                // Download concurrently, throttled to matchPredictionDownloadBatchSize in-flight downloads.
                // Donor ids are partitioned across batches, so batches never share a key and the merge order is irrelevant.
                var merged = new ConcurrentDictionary<int, MatchProbabilityResponse>();

                await Parallel.ForEachAsync(
                    batchResultLocations,
                    new ParallelOptions { MaxDegreeOfParallelism = matchPredictionDownloadBatchSize },
                    async (location, _) =>
                    {
                        var batchResults = await blobDownloader.Download<Dictionary<int, MatchProbabilityResponse>>(matchPredictionResultsContainer, location);
                        foreach (var donorResult in batchResults)
                        {
                            merged[donorResult.Key] = donorResult.Value;
                        }
                    });

                return merged;
            }
        }

        private async Task<Dictionary<int, MatchProbabilityResponse>> DownloadMatchPredictionResults(
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed("Download match prediction algorithm results"))
            {
                logger.SendTrace($"{matchPredictionResultLocations.Count} donor results to download");
                return await blobDownloader.DownloadMultipleBlobs<MatchProbabilityResponse>(matchPredictionResultsContainer, matchPredictionResultLocations, matchPredictionDownloadBatchSize);
            }
        }
    }
}