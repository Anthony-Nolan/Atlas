using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Common.Services.Storers;
using Atlas.MatchPrediction.Test.Validation.Data.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4
{
    internal class SearchResultSetProcessor : ResultSetProcessor<SearchResultsNotification, OriginalSearchResultSet, SearchResult, ValidationSearchRequestRecord>
    {
        private readonly IResultsStorer<SearchResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<SearchResult, LocusMatchDetails> countsStorer;
        private readonly IResultsStorer<SearchResult, MatchedDonorProbability> probabilitiesStorer;

        public SearchResultSetProcessor(
            ISearchRequestsRepository<ValidationSearchRequestRecord> searchRequestsRepository,
            IBlobStreamer resultsStreamer,
            IResultsStorer<SearchResult, MatchedDonor> donorsStorer,
            IResultsStorer<SearchResult, LocusMatchDetails> countsStorer,
            IResultsStorer<SearchResult, MatchedDonorProbability> probabilitiesStorer)
        : base(searchRequestsRepository, resultsStreamer)
        {
            this.donorsStorer = donorsStorer;
            this.countsStorer = countsStorer;
            this.probabilitiesStorer = probabilitiesStorer;
        }

        protected override bool ShouldProcessResult(ValidationSearchRequestRecord searchRequest)
        {
            // process all results
            return true;
        }

        protected override async Task ProcessAndStoreResults(ValidationSearchRequestRecord searchRequest, OriginalSearchResultSet resultSet)
        {
            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await probabilitiesStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
        }

        protected override SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, int numberOfResults)
        {
            return new SuccessfulSearchRequestInfo
            {
                SearchRequestRecordId = searchRequestRecordId,
                MatchedDonorCount = numberOfResults
            };
        }
    }
}