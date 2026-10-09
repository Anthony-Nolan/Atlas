using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.Utility;
using Atlas.MatchingAlgorithm.Settings.Azure;
using AutoFixture;
using AwesomeAssertions;
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
        private IMatchingAlgorithmImportLogger logger;
        private Fixture fixture;

        private IAzureDatabaseManager azureDatabaseManager;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            azureManagementClient = Substitute.For<IAzureDatabaseManagementClient>();
            threadSleeper = Substitute.For<IThreadSleeper>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();

            var defaultOperationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>())
                .Returns(defaultOperationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(new List<DatabaseOperation>
            {
                new DatabaseOperation
                {
                    State = AzureDatabaseOperationState.Succeeded, StartTime = defaultOperationTime
                }
            });

            azureDatabaseManager = new AzureDatabaseManager(
                azureManagementClient,
                threadSleeper,
                new AzureDatabaseManagementSettings
                {
                    ServerName = "server-name",
                    PollingRetryIntervalMilliseconds = "0"
                },
                logger
            );
        }

        [Test]
        public async Task UpdateDatabaseSize_InitiatesDatabaseScaling()
        {
            const string databaseName = "db";
            const AzureDatabaseSize size = AzureDatabaseSize.P15;

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, size, null);

            await azureManagementClient.Received().TriggerDatabaseScaling(databaseName, size, null);
        }

        [Test]
        public async Task UpdateDatabaseSize_PollsOperationsUntilOperationSuccessful()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
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

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null);

            await azureManagementClient.Received(3).GetDatabaseOperations(databaseName);
        }

        [Test]
        public async Task UpdateDatabaseSize_FiresAnInitialQuickPoll()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Succeeded, StartTime = operationTime
                    }
                });

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null);

            threadSleeper.Received(1).Sleep(Arg.Is<int>(sleepTime => sleepTime < 250));
        }

        [Test]
        public async Task UpdateDatabaseSize_WaitsBeforeEachPollOfOperations()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
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

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null);

            threadSleeper.Received(2).Sleep(Arg.Any<int>());
        }

        [Test]
        public async Task UpdateDatabaseSize_WhenPollingForOperationsFailsOnce_RetriesOperationPolling()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                x => throw new Exception(),
                x => new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Succeeded, StartTime = operationTime
                    }
                });

            await azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null);

            await azureManagementClient.Received(2).GetDatabaseOperations(Arg.Any<string>());
        }

        [Test]
        public void UpdateDatabaseSize_WhenPollingForOperationsContinuallyFails_ThrowsException()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Throws(new Exception());

            Assert.ThrowsAsync<Exception>(() => azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null));
        }

        [Test]
        public async Task StartDatabaseScaling_SendsTheChange_AndReturnsTheOperationWithNoPoll()
        {
            var databaseName = fixture.Create<string>();
            var size = fixture.Create<AzureDatabaseSize>();
            var autoPause = fixture.Create<int>();
            var operationTime = fixture.Create<DateTime>();
            azureManagementClient.TriggerDatabaseScaling(databaseName, size, autoPause).Returns(operationTime);

            var operation = await azureDatabaseManager.StartDatabaseScaling(databaseName, size, autoPause);

            operation.Should().Be(new DatabaseScalingOperation(databaseName, size, operationTime));
            await azureManagementClient.DidNotReceiveWithAnyArgs().GetDatabaseOperations(default);
            threadSleeper.DidNotReceiveWithAnyArgs().Sleep(default);
        }

        [Test]
        public async Task StartDatabaseScaling_WhenAzureRefusesTheChange_Throws()
        {
            azureManagementClient.TriggerDatabaseScaling(default, default, default)
                .ThrowsAsyncForAnyArgs(new AzureManagementException(fixture.Create<string>()));

            var act = () => azureDatabaseManager.StartDatabaseScaling(fixture.Create<string>(), fixture.Create<AzureDatabaseSize>(), null);

            await act.Should().ThrowAsync<AzureManagementException>();
        }

        [Test]
        public async Task WaitForDatabaseScaling_PollsUntilTheOperationEnds()
        {
            var operation = fixture.Create<DatabaseScalingOperation>();
            azureManagementClient.GetDatabaseOperations(operation.DatabaseName).Returns(
                OperationsWith(AzureDatabaseOperationState.Pending, operation.StartTime.Value),
                OperationsWith(AzureDatabaseOperationState.InProgress, operation.StartTime.Value),
                OperationsWith(AzureDatabaseOperationState.Succeeded, operation.StartTime.Value));

            await azureDatabaseManager.WaitForDatabaseScaling(operation);

            await azureManagementClient.Received(3).GetDatabaseOperations(operation.DatabaseName);
            await azureManagementClient.DidNotReceiveWithAnyArgs().TriggerDatabaseScaling(default, default, default);
        }

        [Test]
        public async Task WaitForDatabaseScaling_WhenTheOperationFails_Throws()
        {
            var operation = fixture.Create<DatabaseScalingOperation>();
            azureManagementClient.GetDatabaseOperations(operation.DatabaseName)
                .Returns(OperationsWith(AzureDatabaseOperationState.Failed, operation.StartTime.Value));

            var act = () => azureDatabaseManager.WaitForDatabaseScaling(operation);

            await act.Should().ThrowAsync<AzureManagementException>();
        }

        [Test]
        public async Task WaitForDatabaseScaling_WhenNothingWasSent_ReturnsWithNoPoll()
        {
            var operation = new DatabaseScalingOperation(fixture.Create<string>(), fixture.Create<AzureDatabaseSize>(), null);

            await azureDatabaseManager.WaitForDatabaseScaling(operation);

            await azureManagementClient.DidNotReceiveWithAnyArgs().GetDatabaseOperations(default);
        }

        private static List<DatabaseOperation> OperationsWith(AzureDatabaseOperationState state, DateTime startTime) =>
            [new DatabaseOperation { State = state, StartTime = startTime }];

        [Test]
        public void UpdateDatabaseSize_WhenOperationNotSuccessful_ThrowsException()
        {
            const string databaseName = "db";

            var operationTime = DateTime.UtcNow;
            azureManagementClient.TriggerDatabaseScaling(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>()).Returns(operationTime);
            azureManagementClient.GetDatabaseOperations(Arg.Any<string>()).Returns(
                new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        State = AzureDatabaseOperationState.Failed, StartTime = operationTime
                    }
                });

            Assert.ThrowsAsync<AzureManagementException>(() => azureDatabaseManager.UpdateDatabaseSize(databaseName, AzureDatabaseSize.S3, null));
        }
    }
}