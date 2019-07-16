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
        private readonly bool isLocal;

        public AzureDatabaseManager(
            IAzureDatabaseManagementClient databaseManagementClient,
            IThreadSleeper threadSleeper,
            IOptions<AzureDatabaseManagementSettings> settings)
        {
            this.databaseManagementClient = databaseManagementClient;
            this.threadSleeper = threadSleeper;
            isLocal = settings.Value.ServerName == LocalServerName;
        }

        public async Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize)
        {
            if (isLocal)
            {
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }
            
            var operationStartTime = await databaseManagementClient.TriggerDatabaseScaling(databaseName, databaseSize);

            DatabaseOperation databaseOperation;

            do
            {
                threadSleeper.Sleep(OperationPollTimeMilliseconds);
                databaseOperation = await GetDatabaseOperation(databaseName, operationStartTime);
            } while (databaseOperation.State == AzureDatabaseOperationState.InProgress ||
                     databaseOperation.State == AzureDatabaseOperationState.Pending);

            if (databaseOperation.State != AzureDatabaseOperationState.Succeeded)
            {
                throw new AzureManagementException();
            }
        }

        private async Task<DatabaseOperation> GetDatabaseOperation(string databaseName, DateTime operationStartTime)
        {
            var operations = await databaseManagementClient.GetDatabaseOperations(databaseName);
            return operations.Single(o => o.StartTime == operationStartTime);
        }
    }
}