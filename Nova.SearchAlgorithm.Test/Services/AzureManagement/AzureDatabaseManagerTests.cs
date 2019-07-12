using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.Utility;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.AzureManagement
{
    [TestFixture]
    public class AzureDatabaseManagerTests
    {
        private IAzureDatabaseManagementClient azureManagementClient;
        private IThreadSleeper threadSleeper;

        private IAzureDatabaseManager azureDatabaseManager;

        [SetUp]
        public void SetUp()
        {
            azureManagementClient = Substitute.For<IAzureDatabaseManagementClient>();
            threadSleeper = Substitute.For<IThreadSleeper>();

            var defaultOperationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(defaultOperationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(new List<DatabaseOperation>
            {
                new DatabaseOperation
                {
                    State = AzureDatabaseOperationState.Succeeded, StartTime = defaultOperationTime
                }
            });

            azureDatabaseManager = new AzureDatabaseManager(azureManagementClient, threadSleeper);
        }

        [Test]
        public async Task UpdateDatabaseSize_InitiatesDatabaseScaling()
        {
            const string databaseName = "db";
            const AzureDatabaseSize size = AzureDatabaseSize.P15;

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, size);

            await azureManagementClient.Received().TriggerDatabaseScaling(databaseName, size);
        }

        [Test]
        public async Task UpdateDatabaseSize_PollsOperationsUntilOperationSuccessful()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Pending, StartTime = operationTime
                    }
                },
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.InProgress, StartTime = operationTime
                    }
                },
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Succeeded, StartTime = operationTime
                    }
                });

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3);

            await azureManagementClient.Received(3).GetDatabaseOperations(databaseName);
        }

        [Test]
        public async Task UpdateDatabaseSize_WaitsBeforeEachPollOfOperations()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.InProgress, StartTime = operationTime
                    }
                },
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Succeeded, StartTime = operationTime
                    }
                });

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3);

            threadSleeper.Received(2).Sleep(Arg.Any<int>());
        }

        [Test]
        public void UpdateDatabaseSize_WhenOperationNotSuccessful_ThrowsException()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Failed, StartTime = operationTime
                    }
                });

            Assert.ThrowsAsync<AzureManagementException>(() => azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3));
        }
    }
}