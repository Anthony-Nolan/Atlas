using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        Task<SearchResultSet> CombineResults(SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters);
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly ILogger logger;
        private readonly IMatchPredictionResultsDownloader matchPredictionResultsDownloader;
        private readonly string resultsContainer;

        public ResultsCombiner(
            IOptions<AzureStorageSettings> azureStorageSettings,
            ILogger logger,
            IMatchPredictionResultsDownloader matchPredictionResultsDownloader)
        {
            this.logger = logger;
            this.matchPredictionResultsDownloader = matchPredictionResultsDownloader;
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
        }

        /// <inheritdoc />
        public async Task<SearchResultSet> CombineResults(SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters)
        {
            using (logger.RunTimed($"Combine search results: {persistSearchResultsParameters.MatchingAlgorithmResultSet.ResultSet.SearchRequestId}"))
            {
                var matchingResults = persistSearchResultsParameters.MatchingAlgorithmResultSet.ResultSet;
                var matchPredictionResultLocations = persistSearchResultsParameters.MatchPredictionResultLocations.ResultSet;
                var donorInfo = persistSearchResultsParameters.DonorInformation;

                var matchPredictionResults = await DownloadMatchPredictionResults(matchPredictionResultLocations);

                return new SearchResultSet
                {
                    SearchResults = matchingResults.MatchingAlgorithmResults.Select(r => new SearchResult
                    {
                        DonorCode = donorInfo[r.AtlasDonorId].ExternalDonorCode,
                        MatchingResult = r,
                        MatchPredictionResult = matchPredictionResults[r.AtlasDonorId]
                    }),
                    TotalResults = matchingResults.ResultCount,
                    HlaNomenclatureVersion = matchingResults.HlaNomenclatureVersion,
                    SearchRequestId = matchingResults.SearchRequestId,
                    BlobStorageContainerName = resultsContainer,
                    MatchingAlgorithmTime = persistSearchResultsParameters.MatchingAlgorithmResultSet.ElapsedTime,
                    MatchPredictionTime = persistSearchResultsParameters.MatchPredictionResultLocations.ElapsedTime
                };
            }
        }

        private async Task<Dictionary<int, MatchProbabilityResponse>> DownloadMatchPredictionResults(
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed("Download match prediction algorithm results"))
            {
                logger.SendTrace($"{matchPredictionResultLocations.Count} donor results to download");

                var results = await Task.WhenAll(matchPredictionResultLocations.Select(async l =>
                    new KeyValuePair<int, MatchProbabilityResponse>(l.Key, await matchPredictionResultsDownloader.Download(l.Value)))
                );

                return results.ToList().ToDictionary();
            }
        }
    }
}