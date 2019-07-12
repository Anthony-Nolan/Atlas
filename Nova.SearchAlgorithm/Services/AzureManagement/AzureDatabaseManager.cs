using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.Utility;

namespace Nova.SearchAlgorithm.Services.AzureManagement
{
    public interface IAzureDatabaseManager
    {
        Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize);
    }

    public class AzureDatabaseManager : IAzureDatabaseManager
    {
        private const int OperationPollTimeMilliseconds = 60000;

        private readonly IAzureDatabaseManagementClient databaseManagementClient;
        private readonly IThreadSleeper threadSleeper;

        public AzureDatabaseManager(IAzureDatabaseManagementClient databaseManagementClient, IThreadSleeper threadSleeper)
        {
            this.databaseManagementClient = databaseManagementClient;
            this.threadSleeper = threadSleeper;
        }

        public async Task UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize)
        {
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