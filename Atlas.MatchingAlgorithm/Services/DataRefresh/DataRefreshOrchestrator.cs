using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <param name="shouldForceRefresh">
        /// If true, the refresh will occur regardless of whether a new HLA Nomenclature version has been published
        /// </param>
        Task RefreshDataIfNecessary(bool shouldForceRefresh = false);

        /// <summary>
        /// If there is exactly one data refresh in progress, picks up from the last successful stage.
        /// 
        /// This is only intended to be used when a refresh job was interrupted. Calling it when a refresh is actually in progress
        /// would cause two refreshes to be in progress simultaneously, and is not advised.
        ///
        /// This distinction cannot be automated, as there is no difference in the data between "A single job is unfinished, and is actively running",
        /// and "A single job is unfinished, but is not actively running due to an interruption" 
        /// </summary>
        Task ContinueDataRefresh();
    }

    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IHlaMetadataDictionary activeVersionHlaMetadataDictionary;
        private readonly IDataRefreshRunner dataRefreshRunner;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IDataRefreshNotificationSender dataRefreshNotificationSender;

        public DataRefreshOrchestrator(
            IMatchingAlgorithmImportLogger logger,
            DataRefreshSettings dataRefreshSettings,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshRunner dataRefreshRunner,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IAzureDatabaseManager azureDatabaseManager,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IDataRefreshNotificationSender dataRefreshNotificationSender)
        {
            this.logger = logger;
            this.dataRefreshSettings = dataRefreshSettings;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshRunner = dataRefreshRunner;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;

            activeVersionHlaMetadataDictionary = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist()
                ? hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion())
                : null;
        }

        public async Task RefreshDataIfNecessary(bool shouldForceRefresh)
        {
            if (dataRefreshHistoryRepository.GetInProgressJobs().Any())
            {
                logger.SendTrace("Data refresh seems to already be in progress. Data Refresh not started.");
                return;
            }

            // No metadata dictionary will be created if there is no active version - in this case a new version is always available, as there is no active version to compare to.
            var newHlaNomenclatureAvailable = activeVersionHlaMetadataDictionary?.IsActiveVersionDifferentFromLatestVersion() ?? true;
            if (!newHlaNomenclatureAvailable)
            {
                const string noNewData = "No new versions of the WMDA HLA nomenclature have been published.";
                if (shouldForceRefresh)
                {
                    logger.SendTrace(noNewData + " But the refresh was run in 'Forced' mode, so a Data Refresh will start anyway.");
                }
                else
                {
                    logger.SendTrace(noNewData + " Data refresh not started.");
                    return;
                }
            }
            else
            {
                logger.SendTrace("A new version of the WMDA HLA nomenclature has been published. Data Refresh will start.");
            }

            await RunNewDataRefresh();
            logger.SendTrace("Data Refresh ended.");
        }

        /// <inheritdoc />
        public async Task ContinueDataRefresh()
        {
            var inProgressJobs = dataRefreshHistoryRepository.GetInProgressJobs().ToList();
            var inProgressJobCount = inProgressJobs.Count;
            switch (inProgressJobCount)
            {
                case 0:
                    throw new AtlasHttpException(
                        HttpStatusCode.BadRequest,
                        "Cannot continue data refresh, as there are no jobs in progress. Please initiate a non-continue refresh."
                    );
                case 1:
                    //TODO: ATLAS-335: Check continuation 'signature' input.
                    var inProgressJobId = inProgressJobs.Single().Id;
                    await dataRefreshNotificationSender.SendContinuationNotification(inProgressJobId);
                    await dataRefreshHistoryRepository.MarkJobAsContinued(inProgressJobId);
                    await RunDataRefresh(inProgressJobId);
                    break;
                default:
                    throw new AtlasHttpException(
                        HttpStatusCode.BadRequest,
                        "Cannot continue data refresh, as more than one job is in progress. Please manually clean up refresh records."
                    );
            }
        }

        private async Task RunNewDataRefresh()
        {
            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = activeDatabaseProvider.GetDormantDatabase().ToString(),
                RefreshBeginUtc = DateTime.UtcNow,
                HlaNomenclatureVersion = null, //We don't know the version when initially creating the record.
            };
            var recordId = await dataRefreshHistoryRepository.Create(dataRefreshRecord);

            await dataRefreshNotificationSender.SendInitialisationNotification(recordId);

            await RunDataRefresh(recordId);
        }

        private async Task RunDataRefresh(int dataRefreshRecordId)
        {
            try
            {
                var newWmdaHlaNomenclatureVersion = await dataRefreshRunner.RefreshData(dataRefreshRecordId);
                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, true, newWmdaHlaNomenclatureVersion);
                await ScaleDownDatabaseToDormantLevel(previouslyActiveDatabase);
                await dataRefreshNotificationSender.SendSuccessNotification(dataRefreshRecordId);
                logger.SendTrace("Data Refresh Succeeded.");
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendFailureAlert(dataRefreshRecordId);
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, false, null);
            }
        }

        private async Task ScaleDownDatabaseToDormantLevel(string databaseName)
        {
            var dormantSize = dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>();
            logger.SendTrace($"DATA REFRESH TEAR DOWN: Scaling down database: {databaseName} to dormant size: {dormantSize}");
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, dormantSize);
        }

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess, string wmdaHlaNomenclatureVersion)
        {
            await dataRefreshHistoryRepository.UpdateExecutionDetails(recordId, wmdaHlaNomenclatureVersion, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }
    }
}