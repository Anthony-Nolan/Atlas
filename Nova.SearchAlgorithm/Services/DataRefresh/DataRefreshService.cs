using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Performs all pre-processing required for running of the search algorithm:
        /// - Recreates Matching Dictionary
        /// - Imports all donors
        /// - Processes HLA for imported donors
        /// Also coordinates infrastructure changes necessary for the performant running of the data refresh
        /// </summary>
        Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion);
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly DataRefreshSettings settings;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IAzureDatabaseManager azureDatabaseManager;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettings,
            IAzureFunctionManager azureFunctionManager,
            IAzureDatabaseManager azureDatabaseManager)
        {
            this.azureFunctionManager = azureFunctionManager;
            this.azureDatabaseManager = azureDatabaseManager;
            settings = dataRefreshSettings.Value;
        }

        public async Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion)
        {
            await azureFunctionManager.StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
            await azureDatabaseManager.UpdateDatabaseSize(
                GetAzureDatabaseName(databaseToRefresh),
                settings.RefreshDatabaseSize.ToAzureDatabaseSize()
            );
        }

        private string GetAzureDatabaseName(TransientDatabase transientDatabaseType)
        {
            switch (transientDatabaseType)
            {
                case TransientDatabase.DatabaseA:
                    return settings.DatabaseAName;
                case TransientDatabase.DatabaseB:
                    return settings.DatabaseBName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transientDatabaseType), transientDatabaseType, null);
            }
        }
    }
}