using System;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IAzureDatabaseNameProvider
    {
        string GetDatabaseName(TransientDatabase databaseType);
    }
    
    public class AzureDatabaseNameProvider : IAzureDatabaseNameProvider
    {
        private readonly DataRefreshSettings dataRefreshSettings;

        public AzureDatabaseNameProvider(DataRefreshSettings dataRefreshSettings)
        {
            this.dataRefreshSettings = dataRefreshSettings;
        }

        public string GetDatabaseName(TransientDatabase databaseType)
        {
            return databaseType switch
            {
                TransientDatabase.DatabaseA => dataRefreshSettings.DatabaseAName,
                TransientDatabase.DatabaseB => dataRefreshSettings.DatabaseBName,
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null)
            };
        }
    }
}