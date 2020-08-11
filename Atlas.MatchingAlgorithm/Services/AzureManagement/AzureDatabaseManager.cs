using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Polly;

namespace Atlas.MatchingAlgorithm.Services.AzureManagement
{
    public interface IAzureDatabaseManager
    {
        Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize);
    }

    public class AzureDatabaseManager : IAzureDatabaseManager
    {
        private const string LocalServerName = "local";
        private const int OperationPollTimeMilliseconds = 60000;

        private readonly IAzureDatabaseManagementClient databaseManagementClient;
        private readonly IThreadSleeper threadSleeper;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly bool isLocal;
        private readonly long pollingRetryIntervalMilliseconds;

        public AzureDatabaseManager(
            IAzureDatabaseManagementClient databaseManagementClient,
            IThreadSleeper threadSleeper,
            AzureDatabaseManagementSettings settings,
            IMatchingAlgorithmImportLogger logger)
        {
            this.databaseManagementClient = databaseManagementClient;
            this.threadSleeper = threadSleeper;
            this.logger = logger;
            isLocal = settings.ServerName == LocalServerName;
            pollingRetryIntervalMilliseconds = long.Parse(settings.PollingRetryIntervalMilliseconds);
        }

        public async Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize)
        {
            if (isLocal)
            {
                logger.SendTrace("Running locally - will not update database", LogLevel.Verbose);
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            var scaleDescription = $"{databaseName} to size: {databaseSize}";
            using (logger.RunTimed($"Scaling of database: {scaleDescription}", LogLevel.Info, true))
            {
                var operationStartTime = await databaseManagementClient.TriggerDatabaseScaling(databaseName, databaseSize);

                threadSleeper.Sleep(10);
                var databaseOperation = await GetDatabaseOperation(databaseName, operationStartTime);

                while (databaseOperation.State == AzureDatabaseOperationState.InProgress ||
                       databaseOperation.State == AzureDatabaseOperationState.Pending)
                {
                    logger.SendTrace($"Waiting for scaling to complete: {scaleDescription}");
                    threadSleeper.Sleep(OperationPollTimeMilliseconds);
                    databaseOperation = await GetDatabaseOperation(databaseName, operationStartTime);
                }

                if (databaseOperation.State != AzureDatabaseOperationState.Succeeded)
                {
                    logger.SendTrace($"Error scaling {scaleDescription}. State: {databaseOperation.State}");
                    throw new AzureManagementException($"Database scaling operation of {scaleDescription} failed. Check Azure for details");
                }
            }
        }

        private async Task<DatabaseOperation> GetDatabaseOperation(string databaseName, DateTime operationStartTime)
        {
            const int retryCount = 5;
            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(pollingRetryIntervalMilliseconds),
                onRetry: (e, t) =>
                {
                    logger.SendTrace(
                        $"Failed to fetch ongoing database operations with exception {e}. Retrying up to {retryCount} times.",
                        LogLevel.Error
                    );
                });

            return await policy.ExecuteAsync(async () =>
            {
                var operations = await databaseManagementClient.GetDatabaseOperations(databaseName);
                return operations.Single(o => o.StartTime == operationStartTime);
            });
        }
    }
}