using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;
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
        private readonly ILogger logger;
        private readonly bool isLocal;
        private readonly long pollingRetryIntervalMilliseconds;

        public AzureDatabaseManager(
            IAzureDatabaseManagementClient databaseManagementClient,
            IThreadSleeper threadSleeper,
            IOptions<AzureDatabaseManagementSettings> settings,
            ILogger logger)
        {
            this.databaseManagementClient = databaseManagementClient;
            this.threadSleeper = threadSleeper;
            this.logger = logger;
            isLocal = settings.Value.ServerName == LocalServerName;
            pollingRetryIntervalMilliseconds = long.Parse(settings.Value.PollingRetryIntervalMilliseconds);
        }

        public async Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize)
        {
            if (isLocal)
            {
                logger.SendTrace("Running locally - will not update database", LogLevel.Trace);
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            logger.SendTrace($"Initialising scaling of database: {databaseName} to size: {databaseSize}", LogLevel.Info);
            var operationStartTime = await databaseManagementClient.TriggerDatabaseScaling(databaseName, databaseSize);

            DatabaseOperation databaseOperation;

            do
            {
                logger.SendTrace($"Waiting for scaling to complete: {databaseName} to size: {databaseSize}", LogLevel.Info);
                threadSleeper.Sleep(OperationPollTimeMilliseconds);
                databaseOperation = await GetDatabaseOperation(databaseName, operationStartTime);
            } while (databaseOperation.State == AzureDatabaseOperationState.InProgress ||
                     databaseOperation.State == AzureDatabaseOperationState.Pending);

            if (databaseOperation.State != AzureDatabaseOperationState.Succeeded)
            {
                logger.SendTrace($"Error scaling {databaseName} to size: {databaseSize}. State: {databaseOperation.State}", LogLevel.Info);
                throw new AzureManagementException(
                    $"Database scaling operation of {databaseName} to size {databaseSize} failed. Check Azure for details");
            }

            logger.SendTrace($"Finished scaling {databaseName} to size: {databaseSize}", LogLevel.Info);
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