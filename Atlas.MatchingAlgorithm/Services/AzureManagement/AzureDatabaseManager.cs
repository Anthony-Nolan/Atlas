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
        /// <summary>Scales the database, and waits until the operation ends.</summary>
        /// <exception cref="AzureManagementException">Azure refused the change, or the operation failed.</exception>
        Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration);

        /// <summary>
        /// Sends the scale change, and returns when Azure has accepted it: seconds, not the time that the operation takes.
        /// </summary>
        /// <exception cref="AzureManagementException">Azure refused the change.</exception>
        Task<DatabaseScalingOperation> StartDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration);

        /// <summary>Waits until a scale operation from <see cref="StartDatabaseScaling"/> ends.</summary>
        /// <exception cref="AzureManagementException">The operation did not succeed.</exception>
        Task WaitForDatabaseScaling(DatabaseScalingOperation operation);
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

        public async Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration)
        {
            if (isLocal)
            {
                logger.SendTrace("Running locally - will not update database", LogLevel.Verbose);
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            using (logger.RunTimed($"Scaling of database: {DescribeScaling(databaseName, databaseSize)}", LogLevel.Info, true))
            {
                var operation = await StartDatabaseScaling(databaseName, databaseSize, autoPauseDuration);
                await WaitForDatabaseScaling(operation);
            }
        }

        public async Task<DatabaseScalingOperation> StartDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration)
        {
            if (isLocal)
            {
                logger.SendTrace("Running locally - will not update database", LogLevel.Verbose);
                return new DatabaseScalingOperation(databaseName, databaseSize, null);
            }

            var operationStartTime = await databaseManagementClient.TriggerDatabaseScaling(databaseName, databaseSize, autoPauseDuration);
            return new DatabaseScalingOperation(databaseName, databaseSize, operationStartTime);
        }

        public async Task WaitForDatabaseScaling(DatabaseScalingOperation operation)
        {
            if (operation.StartTime is not { } operationStartTime)
            {
                return;
            }

            var databaseName = operation.DatabaseName;
            var scaleDescription = DescribeScaling(databaseName, operation.DatabaseSize);

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

        private static string DescribeScaling(string databaseName, AzureDatabaseSize databaseSize) => $"{databaseName} to size: {databaseSize}";

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