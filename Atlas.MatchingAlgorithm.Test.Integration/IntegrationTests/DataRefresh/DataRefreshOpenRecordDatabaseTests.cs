using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh;

/// <summary>
/// Covers the reads of the open refresh records that a donor genotype precomputation worker makes: the database of each
/// record, for each batch message, and the HLA nomenclature version of each record, for the cache warm-up before the first
/// message. Against a real database, because the filter on open records is in the SQL that the query emits.
/// </summary>
[TestFixture]
internal class DataRefreshOpenRecordDatabaseTests
{
    private Fixture fixture;
    private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
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

    [Test]
    public async Task GetIncompleteRefreshJobHlaNomenclatureVersions_ReturnsTheVersionOfEachOpenRecord()
    {
        var version = fixture.Create<string>();
        var otherVersion = fixture.Create<string>();
        await AnOpenRecord(TransientDatabase.DatabaseA, version);
        await AnOpenRecord(TransientDatabase.DatabaseB, otherVersion);

        var versions = await dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions();

        versions.Should().Contain(version).And.Contain(otherVersion);
    }

    [Test]
    public async Task GetIncompleteRefreshJobHlaNomenclatureVersions_ReturnsEachVersionOnce()
    {
        var version = fixture.Create<string>();
        await AnOpenRecord(TransientDatabase.DatabaseA, version);
        await AnOpenRecord(TransientDatabase.DatabaseB, version);

        var versions = await dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions();

        versions.Should().ContainSingle(returned => returned == version);
    }

    [Test]
    public async Task GetIncompleteRefreshJobHlaNomenclatureVersions_DoesNotReturnTheVersionOfAClosedRecord()
    {
        // A refresh that has ended, whether it succeeded or failed.
        var successfulVersion = fixture.Create<string>();
        var failedVersion = fixture.Create<string>();
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .SuccessfullyCompleted()
            .With(r => r.HlaNomenclatureVersion, successfulVersion)
            .Build());
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .With(r => r.RefreshEndUtc, DateTime.UtcNow)
            .With(r => r.HlaNomenclatureVersion, failedVersion)
            .Build());

        var versions = await dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions();

        versions.Should().NotContain(successfulVersion).And.NotContain(failedVersion);
    }

    [Test]
    public async Task GetIncompleteRefreshJobHlaNomenclatureVersions_DoesNotReturnAnEmptyVersion()
    {
        // A refresh records its version when its HLA Metadata Dictionary stage starts.
        await AnOpenRecord(TransientDatabase.DatabaseA, null);
        await AnOpenRecord(TransientDatabase.DatabaseB, "");

        var versions = await dataRefreshHistoryRepository.GetIncompleteRefreshJobHlaNomenclatureVersions();

        versions.Should().OnlyContain(version => !string.IsNullOrEmpty(version));
    }

    private async Task<int> AnOpenRecord(TransientDatabase database) =>
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.WithDatabase(database).Build());

    private async Task AnOpenRecord(TransientDatabase database, string hlaNomenclatureVersion) =>
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .WithDatabase(database)
            .With(r => r.HlaNomenclatureVersion, hlaNomenclatureVersion)
            .Build());
}
