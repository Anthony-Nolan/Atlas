using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.AzureManagement
{
    [TestFixture]
    public class AzureDatabaseManagerTests
    {
        private IAzureDatabaseManagementClient azureManagementClient;
        private IThreadSleeper threadSleeper;
        private ILogger logger;

        private IAzureDatabaseManager azureDatabaseManager;

        [SetUp]
        public void SetUp()
        {
            azureManagementClient = Substitute.For<IAzureDatabaseManagementClient>();
            threadSleeper = Substitute.For<IThreadSleeper>();
            var settingsOptions = Substitute.For<IOptions<AzureDatabaseManagementSettings>>();
            logger = Substitute.For<ILogger>();

            var defaultOperationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(defaultOperationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(new List<DatabaseOperation>
            {
                new DatabaseOperation
                {
                    State = AzureDatabaseOperationState.Succeeded, StartTime = defaultOperationTime
                }
            });
            settingsOptions.Value.Returns(new AzureDatabaseManagementSettings
            {
                ServerName = "server-name",
                PollingRetryIntervalMilliseconds = "0"
            });

            azureDatabaseManager = new AzureDatabaseManager(
                azureManagementClient,
                threadSleeper,
                settingsOptions,
                logger
            );
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
        public async Task UpdateDatabaseSize_WhenPollingForOperationsFailsOnce_RetriesOperationPolling()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                x => throw new Exception(),
                x => new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Succeeded, StartTime = operationTime
                    }
                });

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3);

            await azureManagementClient.Received(2).GetDatabaseOperations(Arg.Any<string>());
        }

        [Test]
        public void UpdateDatabaseSize_WhenPollingForOperationsContinuallyFails_ThrowsException()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Throws(new Exception());

            Assert.ThrowsAsync<Exception>(() => azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3));
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