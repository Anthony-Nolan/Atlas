using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <summary>
        /// If there is exactly one incomplete data refresh record, the method will pick up from the last successful stage.
        /// 
        /// Calling the method when a refresh is already running would cause two refreshes to be in progress simultaneously,
        /// and is not advised.
        ///
        /// This distinction cannot be automated, as there is no difference in the data between:
        /// "A single job is unfinished, and is actively running", and
        /// "A single job is unfinished, but is not actively running due to an interruption".
        /// </summary>
        Task OrchestrateDataRefresh(int dataRefreshRecordId);
    }

    internal class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IDataRefreshRunner dataRefreshRunner;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IDataRefreshSupportNotificationSender dataRefreshNotificationSender;
        private readonly IDataRefreshCompletionNotifier dataRefreshCompletionNotifier;

        public DataRefreshOrchestrator(
            IMatchingAlgorithmImportLogger logger,
            DataRefreshSettings dataRefreshSettings,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshRunner dataRefreshRunner,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IAzureDatabaseManager azureDatabaseManager,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IDataRefreshSupportNotificationSender dataRefreshNotificationSender,
            IDataRefreshCompletionNotifier dataRefreshCompletionNotifier)
        {
            this.logger = logger;
            this.dataRefreshSettings = dataRefreshSettings;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshRunner = dataRefreshRunner;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            this.dataRefreshCompletionNotifier = dataRefreshCompletionNotifier;
        }

        public async Task OrchestrateDataRefresh(int dataRefreshRecordId)
        {
            var incompleteJob = FetchIncompleteJobRecord(dataRefreshRecordId);

            await dataRefreshNotificationSender.SendInProgressNotification(
                dataRefreshRecordId, 1 + incompleteJob.RefreshAttemptedCount);

            await ContinueRefreshJob(dataRefreshRecordId);
        }

        private DataRefreshRecord FetchIncompleteJobRecord(int dataRefreshRecordId)
        {
            var errorMessagePrefix = $"Cannot run data refresh {dataRefreshRecordId}. ";

            var incompleteJobs = dataRefreshHistoryRepository.GetIncompleteRefreshJobs().ToList();
            switch (incompleteJobs.Count)
            {
                case 0:
                    throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}There is no record of an initiated job. " +
                                                                 "Please submit a new data refresh request.");
                case 1:
                    var incompleteJob = incompleteJobs.Single();
                    if (incompleteJob.Id != dataRefreshRecordId)
                    {
                        throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}In-progress job has ID of {incompleteJob.Id}.");
                    }

                    //TODO: ATLAS-335: Check continuation 'signature' input.
                    return incompleteJob;

                default:
                    throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}More than one open job record found. " +
                                                                 "Please manually clean up refresh records.");
            }
        }

        /// <summary>
        /// Refresh job will be "continued" from the appropriate point, including on the first attempt.
        /// </summary>
        private async Task ContinueRefreshJob(int dataRefreshRecordId)
        {
            try
            {
                await dataRefreshHistoryRepository.UpdateRunAttemptDetails(dataRefreshRecordId);
                var newWmdaHlaNomenclatureVersion = await dataRefreshRunner.RefreshData(dataRefreshRecordId);
                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, true, newWmdaHlaNomenclatureVersion);
                await ScaleDownDatabaseToDormantLevel(previouslyActiveDatabase);
                await dataRefreshCompletionNotifier.NotifyOfSuccess(dataRefreshRecordId);
                logger.SendTrace("Data Refresh Succeeded.");
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);
                await dataRefreshCompletionNotifier.NotifyOfFailure(dataRefreshRecordId);
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