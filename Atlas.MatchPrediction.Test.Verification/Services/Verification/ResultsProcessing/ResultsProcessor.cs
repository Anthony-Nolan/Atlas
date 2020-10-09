using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal interface IResultsProcessor<TDbModel>
    {
        Task ProcessAndStoreResults(int searchRequestRecordId, SearchResultSet resultSet);
    }

    internal abstract class ResultsProcessor<TDbModel> : IResultsProcessor<TDbModel>
    {
        private readonly IProcessedSearchResultsRepository<TDbModel> resultsRepository;

        protected ResultsProcessor(IProcessedSearchResultsRepository<TDbModel> resultsRepository)
        {
            this.resultsRepository = resultsRepository;
        }

        public async Task ProcessAndStoreResults(int searchRequestRecordId, SearchResultSet resultSet)
        {
            if (resultSet.TotalResults == 0)
            {
                return;
            }

            var processedResults = new List<TDbModel>();
            foreach (var searchResult in resultSet.SearchResults)
            {
                processedResults.AddRange(await ProcessSingleSearchResult(searchRequestRecordId, searchResult));
            }

            // important to delete before insertion to wipe any data from previous storage attempts
            await resultsRepository.DeleteResults(searchRequestRecordId);
            await resultsRepository.BulkInsertResults(processedResults);
        }

        protected abstract Task<IEnumerable<TDbModel>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result);
    }
}