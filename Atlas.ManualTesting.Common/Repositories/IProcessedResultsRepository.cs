namespace Atlas.ManualTesting.Common.Repositories
{
    public interface IProcessedResultsRepository<in TDbModel>
    {
        Task DeleteResults(int searchRequestRecordId);
        Task BulkInsert(IEnumerable<TDbModel> results);
    }
}