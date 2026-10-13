using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class DonorGenotypePrecomputationRunCancellerTests
{
    private Fixture fixture;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private FakeLogger<DonorGenotypePrecomputationRunCanceller> logger;

    private DonorGenotypePrecomputationRunCanceller canceller;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        repository = Substitute.For<IDonorGenotypePrecomputationRepository>();
        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(Arg.Any<TransientDatabase>()).Returns(repository);
        logger = new FakeLogger<DonorGenotypePrecomputationRunCanceller>();

        canceller = new DonorGenotypePrecomputationRunCanceller(repositoryFactory, logger);
    }

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public async Task CancelRun_CancelsTheRunOfTheRecordInTheDatabaseOfTheRecord(TransientDatabase database)
    {
        var record = RecordFor(database);

        await canceller.CancelRun(record);

        repositoryFactory.Received(1).GetDonorGenotypePrecomputationRepositoryForDatabase(database);
        repositoryFactory.DidNotReceive().GetDonorGenotypePrecomputationRepositoryForDatabase(database.Other());
        await repository.Received(1).TryMarkRunCancelled(record.Id);
    }

    [Test]
    public async Task CancelRun_RemovesTheStagingDataAfterTheCancel()
    {
        // When the cancel comes first, no worker can start a batch of the run after the removal.
        var record = RecordFor(fixture.Create<TransientDatabase>());
        repository.TryMarkRunCancelled(record.Id).Returns(true);

        await canceller.CancelRun(record);

        Received.InOrder(() =>
        {
            repository.TryMarkRunCancelled(record.Id);
            repository.TruncateStagingTables();
        });
    }

    [Test]
    public async Task CancelRun_WhenTheRecordHasNoRunToCancel_StillRemovesTheStagingData()
    {
        // A refresh that failed in the report of a complete run has no run to cancel, but it has staging data.
        var record = RecordFor(fixture.Create<TransientDatabase>());
        repository.TryMarkRunCancelled(record.Id).Returns(false);

        await canceller.CancelRun(record);

        await repository.Received(1).TruncateStagingTables();
    }

    [Test]
    public async Task CancelRun_WhenTheCancelFails_LogsAnErrorAndDoesNotThrow()
    {
        var record = RecordFor(fixture.Create<TransientDatabase>());
        repository.TryMarkRunCancelled(record.Id).ThrowsAsync(new Exception(fixture.Create<string>()));

        var act = () => canceller.CancelRun(record);

        await act.Should().NotThrowAsync();
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
    }

    [Test]
    public async Task CancelRun_WhenTheStagingDataCannotBeRemoved_LogsAnErrorAndDoesNotThrow()
    {
        var record = RecordFor(fixture.Create<TransientDatabase>());
        repository.TruncateStagingTables().ThrowsAsync(new Exception(fixture.Create<string>()));

        var act = () => canceller.CancelRun(record);

        await act.Should().NotThrowAsync();
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
    }

    [Test]
    public async Task CancelRun_WhenTheRecordNamesNoDatabase_LogsAnErrorAndDoesNotThrow()
    {
        var record = DataRefreshRecordBuilder.New.With(r => r.Id, fixture.Create<int>()).With(r => r.Database, (string)null).Build();

        var act = () => canceller.CancelRun(record);

        await act.Should().NotThrowAsync();
        logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
        repositoryFactory.ReceivedCalls().Should().BeEmpty();
    }

    private DataRefreshRecord RecordFor(TransientDatabase database) =>
        DataRefreshRecordBuilder.New.With(r => r.Id, fixture.Create<int>()).WithDatabase(database).Build();
}
