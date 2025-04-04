﻿using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.ManualTesting.Common.Repositories;

namespace Atlas.ManualTesting.Common.Services.Storers
{
    public interface IResultsStorer<TResult, TDbModel> where TResult : Result
    {
        Task ProcessAndStoreResults(int searchRequestRecordId, ResultSet<TResult> resultSet);
    }

    public abstract class ResultsStorer<TResult, TDbModel> : IResultsStorer<TResult, TDbModel> where TResult : Result
    {
        private readonly IProcessedResultsRepository<TDbModel> resultsRepository;

        protected ResultsStorer(IProcessedResultsRepository<TDbModel> resultsRepository)
        {
            this.resultsRepository = resultsRepository;
        }

        public async Task ProcessAndStoreResults(int searchRequestRecordId, ResultSet<TResult> resultSet)
        {
            if (resultSet.TotalResults == 0)
            {
                return;
            }

            var processedResults = new List<TDbModel>();
            foreach (var result in resultSet.Results)
            {
                processedResults.AddRange(await ProcessSingleSearchResult(searchRequestRecordId, result));
            }

            // important to delete before insertion to wipe any data from previous storage attempts
            await resultsRepository.DeleteResults(searchRequestRecordId);
            await resultsRepository.BulkInsert(processedResults);
        }

        protected abstract Task<IEnumerable<TDbModel>> ProcessSingleSearchResult(int searchRequestRecordId, TResult result);
    }
}