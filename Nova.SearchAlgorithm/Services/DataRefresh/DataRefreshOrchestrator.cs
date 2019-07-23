using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <param name="isContinuedRefresh">
        /// If true, the refresh will not remove existing data, instead only importing / processing new donors.
        /// This should only be triggered manually if a refresh failed
        /// </param>
        Task RefreshDataIfNecessary(bool isContinuedRefresh = false);
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
        private readonly INotificationSender notificationSender;

        public DataRefreshOrchestrator(
            ILogger logger,
            IOptions<DataRefreshSettings> settingsOptions,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshService dataRefreshService,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IAzureFunctionManager azureFunctionManager,
            IAzureDatabaseManager azureDatabaseManager,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            INotificationSender notificationSender)
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
            this.notificationSender = notificationSender;
        }

        public async Task RefreshDataIfNecessary(bool isContinuedRefresh)
        {
            if (!HasNewWmdaDataBeenPublished())
            {
                logger.SendTrace("No new WMDA Hla data has been published. Data refresh not started.", LogLevel.Info);
                return;
            }

            if (IsRefreshInProgress())
            {
                logger.SendTrace("Data refresh is already in progress. Data refresh not started.", LogLevel.Info);
                return;
            }

            await RunDataRefresh(isContinuedRefresh);
        }

        private async Task RunDataRefresh(bool isContinuedRefresh)
        {
            await notificationSender.SendInitialisationNotification();
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
                await dataRefreshService.RefreshData(wmdaDatabaseVersion, isContinuedRefresh);
                await MarkDataHistoryRecordAsComplete(recordId, true);
                await ScaleDownPreviouslyActiveDatabase();
                await notificationSender.SendSuccessNotification();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await notificationSender.SendFailureAlert();
                await MarkDataHistoryRecordAsComplete(recordId, false);
            }
            finally
            {
                await AzureFunctionsTearDown();
            }
        }

        private async Task AzureFunctionsSetUp()
        {
            var donorFunctionsAppName = settingsOptions.Value.DonorFunctionsAppName;
            var donorImportFunctionName = settingsOptions.Value.DonorImportFunctionName;
            logger.SendTrace($"DATA REFRESH SET UP: Disabling donor import function with name: {donorImportFunctionName}", LogLevel.Info);
            await azureFunctionManager.StopFunction(donorFunctionsAppName, donorImportFunctionName);
        }

        private async Task AzureFunctionsTearDown()
        {
            var donorFunctionsAppName = settingsOptions.Value.DonorFunctionsAppName;
            var donorImportFunctionName = settingsOptions.Value.DonorImportFunctionName;
            logger.SendTrace($"DATA REFRESH TEAR DOWN: Re-enabling donor import function with name: {donorImportFunctionName}", LogLevel.Info);
            await azureFunctionManager.StartFunction(donorFunctionsAppName, donorImportFunctionName);
        }

        private async Task ScaleDownPreviouslyActiveDatabase()
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
            var dormantSize = settingsOptions.Value.DormantDatabaseSize.ToAzureDatabaseSize();
            logger.SendTrace($"DATA REFRESH TEAR DOWN: Scaling down database: {databaseName} to dormant size: {dormantSize}", LogLevel.Info);
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, dormantSize);
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