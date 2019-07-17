using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        Task RefreshDataIfNecessary();
    }

    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly ILogger logger;
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private readonly IDataRefreshService dataRefreshService;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;

        public DataRefreshOrchestrator(
            ILogger logger,
            IOptions<DataRefreshSettings> settingsOptions,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshService dataRefreshService,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository, 
            IAzureFunctionManager azureFunctionManager,
            IAzureDatabaseManager azureDatabaseManager,
            IAzureDatabaseNameProvider azureDatabaseNameProvider)
        {
            this.logger = logger;
            this.settingsOptions = settingsOptions;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshService = dataRefreshService;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureFunctionManager = azureFunctionManager;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
        }

        public async Task RefreshDataIfNecessary()
        {
            if (HasNewWmdaDataBeenPublished() && !IsRefreshInProgress())
            {
                await RunDataRefresh();
            }
        }

        private async Task RunDataRefresh()
        {
            var wmdaDatabaseVersion = wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion();

            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = activeDatabaseProvider.GetDormantDatabase().ToString(),
                RefreshBeginUtc = DateTime.UtcNow,
                WmdaDatabaseVersion = wmdaDatabaseVersion,
            };

            var recordId = await dataRefreshHistoryRepository.Create(dataRefreshRecord);

            try
            {
                await AzureFunctionsSetUp();
                await dataRefreshService.RefreshData(wmdaDatabaseVersion);
                await MarkDataHistoryRecordAsComplete(recordId, true);
                await ScaleDownPreviouslyActiveDatabase();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await MarkDataHistoryRecordAsComplete(recordId, false);
            }
            finally
            {
                await AzureFunctionsTearDown();
            }
        }
        
        private async Task AzureFunctionsSetUp()
        {
            var settings = settingsOptions.Value;
            await azureFunctionManager.StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
        }

        private async Task AzureFunctionsTearDown()
        {
            var settings = settingsOptions.Value;
            await azureFunctionManager.StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
        }
        
        private async Task ScaleDownPreviouslyActiveDatabase()
        {
            var settings = settingsOptions.Value;
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, settings.DormantDatabaseSize.ToAzureDatabaseSize());
        }

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess)
        {
            await dataRefreshHistoryRepository.UpdateFinishTime(recordId, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }

        private bool HasNewWmdaDataBeenPublished()
        {
            var activeHlaDataVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();
            var latestHlaDataVersion = wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion();
            return activeHlaDataVersion != latestHlaDataVersion;
        }

        private bool IsRefreshInProgress()
        {
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }
    }
}