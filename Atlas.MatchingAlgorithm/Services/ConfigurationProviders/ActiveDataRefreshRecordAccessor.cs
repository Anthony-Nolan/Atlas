using Atlas.Common.Caching;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using LazyCache;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
{
    public interface IActiveDataRefreshRecordAccessor
    {
        /// <returns>
        /// The most recently completed, successful data refresh record, read once and then cached for the lifetime of
        /// this instance; or null if no refresh has completed yet.
        /// </returns>
        ActiveDataRefreshRecord GetActiveRecord();
    }

    /// <summary>
    /// The single source of the active data refresh record for <see cref="TransientSqlDatabase.ActiveDatabaseProvider"/>
    /// and <see cref="ActiveHlaNomenclatureVersionAccessor"/>.
    /// Reading the active database and the active HLA nomenclature version from one cached record means that they always
    /// describe the same refresh, even if a refresh completes part-way through a request.
    /// </summary>
    /// <remarks>
    /// As with the classes that use it, it is important that this class be injected once per lifetime scope.
    /// </remarks>
    public class ActiveDataRefreshRecordAccessor : IActiveDataRefreshRecordAccessor
    {
        private const string ActiveRecordCacheKey = "activeDataRefreshRecord";

        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAppCache cache;

        public ActiveDataRefreshRecordAccessor(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ITransientCacheProvider cacheProvider)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            cache = cacheProvider.Cache;
        }

        public ActiveDataRefreshRecord GetActiveRecord()
        {
            return cache.GetOrAdd(ActiveRecordCacheKey, () => dataRefreshHistoryRepository.GetActiveRecord());
        }
    }
}
