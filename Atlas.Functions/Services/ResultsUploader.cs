using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface IResultsUploader
    {
        Task UploadResults(MatchingAlgorithmResultSet matchingResults, IDictionary<int, MatchProbabilityResponse> matchPredictionResults);
    }

    internal class ResultsUploader : AzureStorageBlobClient, IResultsUploader
    {
        private readonly string resultsContainer;
        
        /// <inheritdoc />
        public ResultsUploader(IOptions<AzureStorageSettings> azureStorageSettings, ILogger logger) : base(
            azureStorageSettings.Value.ConnectionString, logger)
        {
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
        }

        /// <inheritdoc />
        public async Task UploadResults(MatchingAlgorithmResultSet matchingResults, IDictionary<int, MatchProbabilityResponse> matchPredictionResults)
        {
            var combinedResults = new SearchResultSet
            {
                SearchResults = matchingResults.MatchingAlgorithmResults.Select(r => new SearchResult
                {
                    MatchingResult = r,
                    MatchPredictionResult = matchPredictionResults[r.DonorId].ZeroMismatchProbability
                }),
                TotalResults = matchingResults.ResultCount,
                HlaNomenclatureVersion = matchingResults.HlaNomenclatureVersion,
                SearchRequestId = matchingResults.SearchRequestId,
                BlobStorageContainerName = resultsContainer,
                
            };
            
            var serialisedResults = JsonConvert.SerializeObject(combinedResults);
            await Upload(resultsContainer, combinedResults.ResultsFileName, serialisedResults);
        }
    }
}