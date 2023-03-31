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
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        Task<SearchResultSet> CombineResults(
            ResultSet<MatchingAlgorithmResult> matchingAlgorithmResultSet,
            IReadOnlyDictionary<int, Donor> donorInformation,
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations,
            TimeSpan matchingTime
        );
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly ILogger logger;
        private readonly IBlobDownloader blobDownloader;
        private readonly string resultsContainer;
        private readonly string matchPredictionResultsContainer;

        public ResultsCombiner(
            IOptions<AzureStorageSettings> azureStorageSettings,
            ILogger logger,
            IBlobDownloader blobDownloader)
        {
            this.logger = logger;
            this.blobDownloader = blobDownloader;
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
            matchPredictionResultsContainer = azureStorageSettings.Value.MatchPredictionResultsBlobContainer;
        }

        /// <inheritdoc />
        public async Task<SearchResultSet> CombineResults(
            ResultSet<MatchingAlgorithmResult> matchingAlgorithmResultSet,
            IReadOnlyDictionary<int, Donor> donorInformation,
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations,
            TimeSpan matchingTime
        )
        {
            using (logger.RunTimed($"Combine search results: {matchingAlgorithmResultSet.SearchRequestId}"))
            {
                var matchPredictionResults = await DownloadMatchPredictionResults(matchPredictionResultLocations.ResultSet);

                var resultSet = matchingAlgorithmResultSet is RepeatMatchingAlgorithmResultSet repeatSet
                    ? new RepeatSearchResultSet
                    {
                        RepeatSearchId = repeatSet.RepeatSearchId,
                        NoLongerMatchingDonorCodes = repeatSet.NoLongerMatchingDonors
                    } as SearchResultSet
                    : new OriginalSearchResultSet();

                resultSet.Results = matchingAlgorithmResultSet.Results.Select(r => new SearchResult
                {
                    DonorCode = donorInformation[r.AtlasDonorId].ExternalDonorCode,
                    MatchingResult = r,
                    MatchPredictionResult = matchPredictionResults[r.AtlasDonorId]
                });
                resultSet.TotalResults = matchingAlgorithmResultSet.TotalResults;
                resultSet.MatchingAlgorithmHlaNomenclatureVersion = matchingAlgorithmResultSet.MatchingAlgorithmHlaNomenclatureVersion;
                resultSet.SearchRequestId = matchingAlgorithmResultSet.SearchRequestId;
                resultSet.BlobStorageContainerName = resultsContainer;
                resultSet.MatchingAlgorithmTime = matchingTime;
                resultSet.MatchPredictionTime = matchPredictionResultLocations.ElapsedTime;
                resultSet.SearchRequest = matchingAlgorithmResultSet.SearchRequest;

                return resultSet;
            }
        }

        private async Task<Dictionary<int, MatchProbabilityResponse>> DownloadMatchPredictionResults(
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed("Download match prediction algorithm results"))
            {
                logger.SendTrace($"{matchPredictionResultLocations.Count} donor results to download");
                return await blobDownloader.DownloadMultipleBlobs<MatchProbabilityResponse>(matchPredictionResultsContainer, matchPredictionResultLocations);
            }
        }
    }
}