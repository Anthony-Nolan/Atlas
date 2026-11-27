using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Settings;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking;

[TestFixture]
public class ExpiredResultSetCleanerTests
{
    private Fixture fixture;
    private ICanonicalResultSetRepository repository;
    private IAtlasLogger logger;
    private FakeTimeProvider timeProvider;
    private RepeatSearchSettings settings;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        repository = Substitute.For<ICanonicalResultSetRepository>();
        logger = Substitute.For<IAtlasLogger>();
        timeProvider = new FakeTimeProvider(fixture.Create<DateTimeOffset>().ToUniversalTime());
        settings = new RepeatSearchSettings
        {
            ResultsRetentionDays = fixture.Create<int>(),
            ResultsDeleteBatchSize = fixture.Create<int>(),
            ResultsDeleteMaxRunMinutes = fixture.Create<int>()
        };
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task DeleteExpiredResultSets_WhenRetentionIsOff_DeletesNothing(int retentionDays)
    {
        settings.ResultsRetentionDays = retentionDays;

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().StartDeletingExpiredResultSet(default, default);
        await repository.DidNotReceiveWithAnyArgs().DeleteResultsBatch(default, default, default);
        await repository.DidNotReceiveWithAnyArgs().DeleteResultSet(default, default);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task DeleteExpiredResultSets_WhenBatchSizeIsBelowOne_ClaimsNoSetAndLogsWarning(int batchSize)
    {
        settings.ResultsDeleteBatchSize = batchSize;

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().StartDeletingExpiredResultSet(default, default);
        await repository.DidNotReceiveWithAnyArgs().DeleteResultsBatch(default, default, default);
        logger.Received(1).SendTrace(
            Arg.Is<string>(m => m.Contains(nameof(RepeatSearchSettings.ResultsDeleteBatchSize))),
            LogLevel.Warn,
            Arg.Any<Dictionary<string, string>>());
    }

    [Test]
    public async Task DeleteExpiredResultSets_UsesCutOffOfRetentionDaysBeforeNow()
    {
        var expectedCutOff = timeProvider.GetUtcNow().AddDays(-settings.ResultsRetentionDays);

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.Received(1).StartDeletingExpiredResultSet(expectedCutOff, CancellationToken.None);
    }

    [Test]
    public async Task DeleteExpiredResultSets_DeletesBatchesUntilShortBatch_ThenDeletesSetRow()
    {
        var setId = fixture.Create<int>();
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId, (int?)null);
        repository.DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None)
            .Returns(settings.ResultsDeleteBatchSize, settings.ResultsDeleteBatchSize, settings.ResultsDeleteBatchSize - 1);

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.Received(3).DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None);
        await repository.Received(1).DeleteResultSet(setId, CancellationToken.None);
    }

    [Test]
    public async Task DeleteExpiredResultSets_DeletesEveryExpiredSet_ThenStops()
    {
        var setIds = fixture.CreateMany<int>(2).ToArray();
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setIds[0], setIds[1], null);
        repository.DeleteResultsBatch(default, default, default).ReturnsForAnyArgs(0);

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.Received(1).DeleteResultSet(setIds[0], CancellationToken.None);
        await repository.Received(1).DeleteResultSet(setIds[1], CancellationToken.None);
        await repository.ReceivedWithAnyArgs(3).StartDeletingExpiredResultSet(default, default);
    }

    [Test]
    public async Task DeleteExpiredResultSets_PassesCancellationTokenToEachSqlCall()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var setId = fixture.Create<int>();
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId, (int?)null);
        repository.DeleteResultsBatch(default, default, default).ReturnsForAnyArgs(0);

        await BuildCleaner().DeleteExpiredResultSets(token);

        await repository.Received(2).StartDeletingExpiredResultSet(Arg.Any<DateTimeOffset>(), token);
        await repository.Received(1).DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, token);
        await repository.Received(1).DeleteResultSet(setId, token);
    }

    [Test]
    public async Task DeleteExpiredResultSets_AtTimeLimit_StopsWithoutDeletingSetRow()
    {
        var setId = fixture.Create<int>();
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId);
        repository.DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None).Returns(_ =>
        {
            timeProvider.Advance(TimeSpan.FromMinutes(settings.ResultsDeleteMaxRunMinutes));
            return settings.ResultsDeleteBatchSize;
        });

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        await repository.Received(1).DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None);
        await repository.DidNotReceiveWithAnyArgs().DeleteResultSet(default, default);
        logger.Received(1).SendEvent(
            ExpiredResultSetCleaner.DeletedEventName,
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["Result"] == "TimeLimit"),
            Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public async Task DeleteExpiredResultSets_SendsEventWithCounts()
    {
        var setId = fixture.Create<int>();
        var lastBatchSize = settings.ResultsDeleteBatchSize - 1;
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId, (int?)null);
        repository.DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None)
            .Returns(settings.ResultsDeleteBatchSize, lastBatchSize);

        await BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        logger.Received(1).SendEvent(
            ExpiredResultSetCleaner.DeletedEventName,
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["Result"] == "Completed"),
            Arg.Is<Dictionary<string, double>>(m =>
                m["SetCount"] == 1 && m["RowCount"] == settings.ResultsDeleteBatchSize + lastBatchSize));
    }

    [Test]
    public async Task DeleteExpiredResultSets_WhenCancelled_ThrowsBeforeDeletingAndSendsEvent()
    {
        var act = () => BuildCleaner().DeleteExpiredResultSets(new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
        await repository.DidNotReceiveWithAnyArgs().DeleteResultsBatch(default, default, default);
        logger.Received(1).SendEvent(
            ExpiredResultSetCleaner.DeletedEventName,
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["Result"] == "Cancelled"),
            Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public async Task DeleteExpiredResultSets_WhenCancelledDuringBatch_RethrowsAndSendsCancelledEvent()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var setId = fixture.Create<int>();
        // Not an OperationCanceledException: SqlClient can report a cancel during a statement as a SQL error.
        var exception = new InvalidOperationException(fixture.Create<string>());
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId);
        repository.DeleteResultsBatch(default, default, default).ReturnsForAnyArgs(_ =>
        {
            cancellationTokenSource.Cancel();
            return Task.FromException<int>(exception);
        });

        var act = () => BuildCleaner().DeleteExpiredResultSets(cancellationTokenSource.Token);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        logger.Received(1).SendEvent(
            ExpiredResultSetCleaner.DeletedEventName,
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["Result"] == "Cancelled"),
            Arg.Is<Dictionary<string, double>>(m => m["SetCount"] == 0 && m["RowCount"] == 0));
    }

    [Test]
    public async Task DeleteExpiredResultSets_WhenBatchFails_RethrowsAndSendsEventWithRowsDeletedSoFar()
    {
        var setId = fixture.Create<int>();
        var exception = new InvalidOperationException(fixture.Create<string>());
        repository.StartDeletingExpiredResultSet(default, default).ReturnsForAnyArgs(setId);
        repository.DeleteResultsBatch(setId, settings.ResultsDeleteBatchSize, CancellationToken.None).Returns(
            _ => settings.ResultsDeleteBatchSize,
            _ => throw exception);

        var act = () => BuildCleaner().DeleteExpiredResultSets(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        logger.Received(1).SendEvent(
            ExpiredResultSetCleaner.DeletedEventName,
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["Result"] == "Failed"),
            Arg.Is<Dictionary<string, double>>(m => m["SetCount"] == 0 && m["RowCount"] == settings.ResultsDeleteBatchSize));
    }

    private ExpiredResultSetCleaner BuildCleaner() => new(repository, settings, timeProvider, logger);
}
