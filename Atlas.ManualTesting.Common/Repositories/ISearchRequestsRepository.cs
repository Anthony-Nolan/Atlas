using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Models.Entities;

namespace Atlas.ManualTesting.Common.Repositories
{
    public interface ISearchRequestsRepository<TRecord> where TRecord : SearchRequestRecord
    {
        Task AddSearchRequest(TRecord request);
        Task<TRecord?> GetRecordByAtlasSearchId(string atlasSearchId);
        Task MarkSearchResultsAsFailed(int searchRequestRecordId);
        Task MarkSearchResultsAsSuccessful(SuccessfulSearchRequestInfo info);
    }
}