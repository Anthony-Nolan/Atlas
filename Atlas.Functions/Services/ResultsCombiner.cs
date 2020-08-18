using System.Linq;
using Atlas.Client.Models.Search.Results;
using Atlas.Functions.DurableFunctions.Search.Activity;
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
        public SearchResultSet CombineResults(
            SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters)
        {
            var matchingResults = persistSearchResultsParameters.MatchingAlgorithmResultSet.ResultSet;
            var matchPredictionResults = persistSearchResultsParameters.MatchPredictionResults.ResultSet;
            var donorInfo = persistSearchResultsParameters.DonorInformation;
            
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
                MatchPredictionTime = persistSearchResultsParameters.MatchPredictionResults.ElapsedTime
            };
        }
    }
}