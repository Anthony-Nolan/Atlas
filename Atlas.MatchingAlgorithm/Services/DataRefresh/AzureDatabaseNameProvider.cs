using System;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IAzureDatabaseNameProvider
    {
        string GetDatabaseName(TransientDatabase databaseType);
    }
    
    public class AzureDatabaseNameProvider : IAzureDatabaseNameProvider
    {
        private readonly IOptions<DataRefreshSettings> settingsOptions;

        public AzureDatabaseNameProvider(IOptions<DataRefreshSettings> settingsOptions)
        {
            this.settingsOptions = settingsOptions;
        }

        public string GetDatabaseName(TransientDatabase databaseType)
        {
                var settings = settingsOptions.Value;

                switch (databaseType)
                {
                    case TransientDatabase.DatabaseA:
                        return settings.DatabaseAName;
                    case TransientDatabase.DatabaseB:
                        return settings.DatabaseBName;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null);
                }
        }
    }
}