using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <param name="shouldForceRefresh">
        /// If true, the refresh will occur regardless of whether a new hla database version has been published
        /// </param>
        Task RefreshDataIfNecessary(bool shouldForceRefresh = false);
    }

    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly ILogger logger;
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
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
            IHlaMetadataDictionary hlaMetadataDictionary,
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
            this.hlaMetadataDictionary = hlaMetadataDictionary;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshService = dataRefreshService;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureFunctionManager = azureFunctionManager;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
        }

        public async Task RefreshDataIfNecessary(bool shouldForceRefresh)
        {
            if (IsRefreshInProgress())
            {
                logger.SendTrace("Data refresh is already in progress. Data refresh not started.", LogLevel.Info);
                return;
            }

            var newWmdaVersionAvailable = hlaMetadataDictionary.IsRefreshNecessary();
            if (!newWmdaVersionAvailable)
            {
                if (shouldForceRefresh)
                {
                    logger.SendTrace("No new WMDA Hla data has been published, but refresh has been forced. Data refresh will start.", LogLevel.Info);
                }
                else
                {
                    logger.SendTrace("No new WMDA Hla data has been published. Data refresh not started.", LogLevel.Info);
                    return;
                }
            }
            else
            {
                logger.SendTrace("New WMDA Hla data is available. Data refresh will start.", LogLevel.Info);
            }

            await RunDataRefresh();
            logger.SendTrace("Data refresh ended.", LogLevel.Info);
        }

        private async Task RunDataRefresh()
        {
            await dataRefreshNotificationSender.SendInitialisationNotification();

            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = activeDatabaseProvider.GetDormantDatabase().ToString(),
                RefreshBeginUtc = DateTime.UtcNow,
                WmdaDatabaseVersion = null,
            };

            var recordId = await dataRefreshHistoryRepository.Create(dataRefreshRecord);

            try
            {
                await AzureFunctionsSetUp();
                var newWmdaDataVersion = await dataRefreshService.RefreshData();
                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(recordId, true, newWmdaDataVersion);
                await ScaleDownDatabaseToDormantLevel(previouslyActiveDatabase);
                await dataRefreshNotificationSender.SendSuccessNotification();
                logger.SendTrace("Data Refresh Succeeded.", LogLevel.Info);
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendFailureAlert();
                await MarkDataHistoryRecordAsComplete(recordId, false, null);
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

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess, string wmdaDataVersion)
        {
            await dataRefreshHistoryRepository.UpdateExecutionDetails(recordId, wmdaDataVersion, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }

        private bool IsRefreshInProgress()
        {
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }
    }
}