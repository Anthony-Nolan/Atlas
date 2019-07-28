using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.Utility;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.AzureManagement
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
            var operations = await databaseManagementClient.GetDatabaseOperations(databaseName);
            return operations.Single(o => o.StartTime == operationStartTime);
        }
    }
}