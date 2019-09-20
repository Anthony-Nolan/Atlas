using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshCleanupService
    {
        /// <summary>
        /// Runs appropriate clean up after a data refresh job - i.e. scaling down the dormant database, and re-enabling donor update functions.
        /// This should only ever be run manually, in the case of the server dying mid-data refresh, and being unable to run the teardown as part of that.
        /// </summary>
        Task RunDataRefreshCleanup();

        /// <summary>
        /// Will decide whether we think clean up needs to be run, and if so send a notification for the support team.
        /// This is only accurate if the following are true:
        /// - This class is called from the same service as runs the data refresh
        /// - That service is a single instance, always-on application
        /// - This is only called on startup of the service
        /// </summary>
        Task SendCleanupRecommendation();
    }

    public class DataRefreshCleanupService : IDataRefreshCleanupService
    {
        private const string NotificationOriginator = "Nova.SearchAlgorithm";

        private readonly ILogger logger;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IOptions<DataRefreshSettings> dataRefreshSettings;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly INotificationsClient notificationsClient;

        public DataRefreshCleanupService(
            ILogger logger,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IOptions<DataRefreshSettings> dataRefreshSettings,
            IAzureFunctionManager azureFunctionManager,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            INotificationsClient notificationsClient
        )
        {
            this.logger = logger;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            this.dataRefreshSettings = dataRefreshSettings;
            this.azureFunctionManager = azureFunctionManager;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.notificationsClient = notificationsClient;
        }

        public async Task RunDataRefreshCleanup()
        {
            if (IsCleanupNecessary())
            {
                const string notificationSummary =
                    "DATA REFRESH: Manual Teardown requested. This indicates that the data refresh failed unexpectedly.";
                logger.SendTrace(notificationSummary, LogLevel.Info);
                await notificationsClient.SendNotification(new Notification(
                    notificationSummary,
                    "A manual teardown was requested, and the search algorithm has detected ongoing data-refresh jobs - this should have been called if the app restarted unexpectedly during a data refresh. " +
                    "Appropriate teardown is being run. The data refresh will need to be re-started once the reason for the server restart has been diagnosed and handled. " +
                    "Possible causes could include: (a) the service plan running out of memory (b) an azure outage (c) a deployment of the algorithm service.",
                    NotificationOriginator)
                );
                await ScaleDatabase();
                await EnableDonorManagementFunction();
                await UpdateDataRefreshHistoryRecords();
            }
            else
            {
                logger.SendTrace(
                    "Data Refresh cleanup triggered, but no in progress jobs detected. Are you sure cleanup is necessary?",
                    LogLevel.Info
                );
            }
        }

        public async Task SendCleanupRecommendation()
        {
            if (IsCleanupNecessary())
            {
                await notificationsClient.SendAlert(new Alert(
                    "Data Refresh: Manual cleanup recommended.",
                    "The algorithm has detected an in-progress data refresh job on startup. This generally implies that a data refresh job " +
                    "was terminated without the appropriate teardown being run - this should only happen if the service was re-started unexpectedly. " +
                    "Possible causes could include: (a) the service plan running out of memory (b) an azure outage (c) a deployment of the algorithm service. " +
                    "We should confirm that this was the case, and if so, run appropriate clean-up. See the README of the Nova.SearchAlgorithm project for more information. " +
                    "The function `RunDataRefreshCleanup` should encapsulate the majority of the necessary clean-up. " +
                    "CAVEAT: Due to restrictions of triggers in Azure functions, this function will run once a year not at start-up. " +
                    "Check the crontab of the `CheckIfCleanupNecessary` function to ensure it isn't this known false positive.'",
                    Priority.High,
                    NotificationOriginator
                ));
            }
        }

        private bool IsCleanupNecessary()
        {
            // This assumes that the method will not be called when a job is actually in progress, only when a job has finished without appropriate teardown 
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }

        private async Task ScaleDatabase()
        {
            var targetSize = dataRefreshSettings.Value.DormantDatabaseSize.ToAzureDatabaseSize();
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            logger.SendTrace($"DATA REFRESH CLEANUP: Scaling database: {databaseName} to size {targetSize}", LogLevel.Info);
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }

        private async Task EnableDonorManagementFunction()
        {
            var donorFunctionsAppName = dataRefreshSettings.Value.DonorFunctionsAppName;
            var donorImportFunctionName = dataRefreshSettings.Value.DonorImportFunctionName;
            logger.SendTrace($"DATA REFRESH CLEANUP: Re-enabling donor import function with name: {donorImportFunctionName}", LogLevel.Info);
            await azureFunctionManager.StartFunction(donorFunctionsAppName, donorImportFunctionName);
        }

        private async Task UpdateDataRefreshHistoryRecords()
        {
            var dataRefreshRecords = dataRefreshHistoryRepository.GetInProgressJobs().ToList();
            foreach (var job in dataRefreshRecords)
            {
                await dataRefreshHistoryRepository.UpdateFinishTime(job.Id, DateTime.UtcNow);
                await dataRefreshHistoryRepository.UpdateSuccessFlag(job.Id, false);
            }
        }
    }
}