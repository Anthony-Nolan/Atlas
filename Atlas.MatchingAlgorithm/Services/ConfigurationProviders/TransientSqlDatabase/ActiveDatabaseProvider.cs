using Atlas.MatchingAlgorithm.Data.Persistent.Models;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase
{
    public interface IActiveDatabaseProvider
    {
        TransientDatabase GetActiveDatabase();
        TransientDatabase GetDormantDatabase();
    }

    public class ActiveDatabaseProvider : IActiveDatabaseProvider
    {
        private readonly IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;

        public ActiveDatabaseProvider(IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor)
        {
            this.activeDataRefreshRecordAccessor = activeDataRefreshRecordAccessor;
        }

        public TransientDatabase GetActiveDatabase()
        {
            // The accessor caches the active record rather than fetching it every time, which means that all queries within
            // the lifetime of this class will access the same database, even if the refresh job finishes mid-request.
            // As such it is especially important that this class be injected once per lifetime scope (i.e. singleton per http request)
            return activeDataRefreshRecordAccessor.GetActiveRecord()?.Database ?? TransientDatabase.DatabaseA;
        }

        public TransientDatabase GetDormantDatabase()
        {
            return GetActiveDatabase().Other();
        }
    }
}
