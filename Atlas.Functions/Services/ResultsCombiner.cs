using System.Linq;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IResultsCombiner
    {
        SearchResultSet CombineResults(SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters);
    }

    internal class ResultsCombiner : IResultsCombiner
    {
        private readonly string resultsContainer;

        public ResultsCombiner(IOptions<AzureStorageSettings> azureStorageSettings)
        {
            resultsContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
        }

        
        /// <inheritdoc />
        public SearchResultSet CombineResults(SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters)
        {
            var matchingResults = persistSearchResultsParameters.MatchingAlgorithmResultSet;
            var matchPredictionResults = persistSearchResultsParameters.MatchPredictionResults;
            var donorInfo = persistSearchResultsParameters.DonorInformation;
            
            return new SearchResultSet
            {
                SearchResults = matchingResults.MatchingAlgorithmResults.Select(r => new SearchResult
                {
                    DonorCode = donorInfo[r.AtlasDonorId].ExternalDonorCode,
                    MatchingResult = r,
                    MatchPredictionResult = matchPredictionResults[r.AtlasDonorId].ZeroMismatchProbability
                }),
                TotalResults = matchingResults.ResultCount,
                HlaNomenclatureVersion = matchingResults.HlaNomenclatureVersion,
                SearchRequestId = matchingResults.SearchRequestId,
                BlobStorageContainerName = resultsContainer,
            };
        }
    }
}