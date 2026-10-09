using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh;

/// <summary>
/// Covers the read of the database of each open refresh record, which a donor genotype precomputation worker makes for
/// each batch message. Against a real database, because the filter on open records is in the SQL that the query emits.
/// </summary>
[TestFixture]
internal class DataRefreshOpenRecordDatabaseTests
{
    private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

    [SetUp]
    public void SetUp()
    {
        dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
        IntegrationTestSetUp.RunInitialDataRefresh();
    }

    [Test]
    public async Task GetIncompleteRefreshJobDatabases_ReturnsTheDatabaseOfEachOpenRecord()
    {
        var recordOfDatabaseA = await AnOpenRecord(TransientDatabase.DatabaseA);
        var recordOfDatabaseB = await AnOpenRecord(TransientDatabase.DatabaseB);

        var databases = await dataRefreshHistoryRepository.GetIncompleteRefreshJobDatabases();

        databases.Should().Contain(recordOfDatabaseA, TransientDatabase.DatabaseA)
            .And.Contain(recordOfDatabaseB, TransientDatabase.DatabaseB);
    }

    [Test]
    public async Task GetIncompleteRefreshJobDatabases_DoesNotReturnAClosedRecord()
    {
        // A refresh that has ended, whether it succeeded or failed.
        var successfulRecord = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.SuccessfullyCompleted().Build());
        var failedRecord = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .With(r => r.RefreshEndUtc, DateTime.UtcNow)
            .Build());

        var databases = await dataRefreshHistoryRepository.GetIncompleteRefreshJobDatabases();

        databases.Should().NotContainKey(successfulRecord).And.NotContainKey(failedRecord);
    }

    private async Task<int> AnOpenRecord(TransientDatabase database) =>
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.WithDatabase(database).Build());
}
