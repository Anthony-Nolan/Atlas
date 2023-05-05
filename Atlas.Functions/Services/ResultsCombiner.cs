using System;
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
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        Task<IEnumerable<SearchResult>> CombineResults(
            string searchRequestId,
            IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults,
            IReadOnlyDictionary<int, Donor> donorInformation,
            IReadOnlyDictionary<int, string> matchPredictionResultLocations);

        SearchResultSet BuildResultsSummary(ResultSet<MatchingAlgorithmResult> matchingAlgorithmResultSet, TimeSpan matchPredictionTime, TimeSpan matchingTime);
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly ILogger logger;
        private readonly IBlobDownloader blobDownloader;
        private readonly string resultsContainer;
        private readonly string matchPredictionResultsContainer;
        private readonly int matchPredictionDownloadBatchSize;

        public ResultsCombiner(
            IOptions<AzureStorageSettings> azureStorageSettings,
            ISearchLogger<SearchLoggingContext> logger,
            IBlobDownloader blobDownloader)
        {
            this.logger = logger;
            this.blobDownloader = blobDownloader;
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
            IReadOnlyDictionary<int, Donor> donorInformation,
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed($"Combine search results: {searchRequestId}"))
            {
                var matchPredictionResults = await DownloadMatchPredictionResults(matchPredictionResultLocations);
                return matchingAlgorithmResults.Select(r => new SearchResult
                {
                    DonorCode = donorInformation[r.AtlasDonorId].ExternalDonorCode,
                    MatchingResult = r,
                    MatchPredictionResult = matchPredictionResults[r.AtlasDonorId]
                });
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