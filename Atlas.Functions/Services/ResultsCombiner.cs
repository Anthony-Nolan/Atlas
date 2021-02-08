using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.Models;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        Task<SearchResultSet> CombineResults(
            MatchingAlgorithmResultSet matchingAlgorithmResultSet,
            IReadOnlyDictionary<int, Donor> donorInformation,
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations,
            TimeSpan matchingTime
        );
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
        public async Task<SearchResultSet> CombineResults(
            MatchingAlgorithmResultSet matchingAlgorithmResultSet,
            IReadOnlyDictionary<int, Donor> donorInformation,
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations,
            TimeSpan matchingTime
        )
        {
            using (logger.RunTimed($"Combine search results: {matchingAlgorithmResultSet.SearchRequestId}"))
            {
                var matchPredictionResults = await DownloadMatchPredictionResults(matchPredictionResultLocations.ResultSet);

                return new SearchResultSet
                {
                    SearchResults = matchingAlgorithmResultSet.MatchingAlgorithmResults.Select(r => new SearchResult
                    {
                        DonorCode = donorInformation[r.AtlasDonorId].ExternalDonorCode,
                        MatchingResult = r,
                        MatchPredictionResult = matchPredictionResults[r.AtlasDonorId]
                    }),
                    TotalResults = matchingAlgorithmResultSet.ResultCount,
                    HlaNomenclatureVersion = matchingAlgorithmResultSet.HlaNomenclatureVersion,
                    SearchRequestId = matchingAlgorithmResultSet.SearchRequestId,
                    RepeatSearchId = matchingAlgorithmResultSet.RepeatSearchId,
                    BlobStorageContainerName = resultsContainer,
                    MatchingAlgorithmTime = matchingTime,
                    MatchPredictionTime = matchPredictionResultLocations.ElapsedTime
                };
            }
        }

        private async Task<Dictionary<int, MatchProbabilityResponse>> DownloadMatchPredictionResults(
            IReadOnlyDictionary<int, string> matchPredictionResultLocations)
        {
            using (logger.RunTimed("Download match prediction algorithm results"))
            {
                logger.SendTrace($"{matchPredictionResultLocations.Count} donor results to download");

                var results = new List<KeyValuePair<int, MatchProbabilityResponse>>();

                // Batch downloads to avoid using too many outbound connections
                foreach (var resultLocationBatch in matchPredictionResultLocations.Batch(100))
                {
                    var resultBatch = await Task.WhenAll(resultLocationBatch.Select(async l =>
                        new KeyValuePair<int, MatchProbabilityResponse>(l.Key, await matchPredictionResultsDownloader.Download(l.Value)))
                    );
                    results.AddRange(resultBatch);
                }

                return results.ToList().ToDictionary();
            }
        }
    }
}