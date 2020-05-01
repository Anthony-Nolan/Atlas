using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <param name="shouldForceRefresh">
        /// If true, the refresh will occur regardless of whether a new hla database version has been published
        /// </param>
        /// <param name="isContinuedRefresh">
        /// If true, the refresh will not remove existing data, instead only importing / processing new donors.
        /// This should only be triggered manually if a refresh failed
        /// </param>
        Task RefreshDataIfNecessary(bool shouldForceRefresh = false, bool isContinuedRefresh = false);
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
        private readonly IDataRefreshNotificationSender dataRefreshNotificationSender;

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
            IDataRefreshNotificationSender dataRefreshNotificationSender)
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
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
        }

        public async Task RefreshDataIfNecessary(bool shouldForceRefresh, bool isContinuedRefresh)
        {
            if (!shouldForceRefresh && !HasNewWmdaDataBeenPublished())
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
            await dataRefreshNotificationSender.SendInitialisationNotification();
            var wmdaDatabaseVersion = wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();

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
                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(recordId, true);
                await ScaleDownDatabaseToDormantLevel(previouslyActiveDatabase);
                await dataRefreshNotificationSender.SendSuccessNotification();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendFailureAlert();
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

        private async Task ScaleDownDatabaseToDormantLevel(string databaseName)
        {
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
            var latestHlaDataVersion = wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();
            return activeHlaDataVersion != latestHlaDataVersion;
        }

        private bool IsRefreshInProgress()
        {
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }
    }
}