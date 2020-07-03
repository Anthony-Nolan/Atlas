using System.Collections.Generic;
using System.Linq;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Settings;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        SearchResultSet CombineResults(MatchingAlgorithmResultSet matchingResults, IDictionary<int, MatchProbabilityResponse> matchPredictionResults);
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly string resultsContainer;

        public ResultsCombiner(IOptions<AzureStorageSettings> azureStorageSettings)
        {
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
        }

        
        /// <inheritdoc />
        public SearchResultSet CombineResults(MatchingAlgorithmResultSet matchingResults, IDictionary<int, MatchProbabilityResponse> matchPredictionResults)
        {
            return new SearchResultSet
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
        }
    }
}