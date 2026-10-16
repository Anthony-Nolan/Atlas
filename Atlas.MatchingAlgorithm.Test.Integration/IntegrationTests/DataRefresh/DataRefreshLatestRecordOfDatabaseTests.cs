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
/// Covers the read of the latest refresh record that wrote to a database, which gives the genotype set debug endpoint the
/// nomenclature version of the dormant database. Against a real database, because the filter is in the SQL that the
/// query emits.
/// </summary>
[TestFixture]
internal class DataRefreshLatestRecordOfDatabaseTests
{
    private Fixture fixture;
    private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

    [SetUp]
    public async Task SetUp()
    {
        fixture = new Fixture();
        dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
        await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
    }

    [TearDown]
    public async Task TearDown()
    {
        await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
        IntegrationTestSetUp.RunInitialDataRefresh();
    }

    [Test]
    public async Task GetLatestRecordThatWroteToDatabase_ReturnsTheOpenRefreshThatIsFillingTheDatabase()
    {
        await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.QueuedDonorUpdateProcessing, isOpen: false);
        var filling = await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.IndexRecreation, isOpen: true);

        var record = await dataRefreshHistoryRepository.GetLatestRecordThatWroteToDatabase(TransientDatabase.DatabaseB);

        record.Id.Should().Be(filling.Id);
        record.HlaNomenclatureVersion.Should().Be(filling.HlaNomenclatureVersion);
    }

    [Test]
    public async Task GetLatestRecordThatWroteToDatabase_ReturnsALastRefreshThatFailedAfterItDeletedTheData()
    {
        var failed = await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.DonorImport, isOpen: false);

        var record = await dataRefreshHistoryRepository.GetLatestRecordThatWroteToDatabase(TransientDatabase.DatabaseB);

        record.Id.Should().Be(failed.Id);
    }

    [Test]
    public async Task GetLatestRecordThatWroteToDatabase_IgnoresNewerRecordsThatHaveNotDeletedTheData()
    {
        // One is still at an earlier stage, and one stopped before: the data in the database is still from the first.
        var wrote = await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.QueuedDonorUpdateProcessing, isOpen: false);
        await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.IndexRemoval, isOpen: false);
        await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.MetadataDictionaryRefresh, isOpen: true);

        var record = await dataRefreshHistoryRepository.GetLatestRecordThatWroteToDatabase(TransientDatabase.DatabaseB);

        record.Id.Should().Be(wrote.Id);
        record.HlaNomenclatureVersion.Should().Be(wrote.HlaNomenclatureVersion);
    }

    [Test]
    public async Task GetLatestRecordThatWroteToDatabase_IgnoresTheRecordsOfTheOtherDatabase()
    {
        var ofDatabaseB = await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.QueuedDonorUpdateProcessing, isOpen: false);
        await ARecord(TransientDatabase.DatabaseA, DataRefreshStage.QueuedDonorUpdateProcessing, isOpen: false);

        var record = await dataRefreshHistoryRepository.GetLatestRecordThatWroteToDatabase(TransientDatabase.DatabaseB);

        record.Id.Should().Be(ofDatabaseB.Id);
    }

    [Test]
    public async Task GetLatestRecordThatWroteToDatabase_WhenNoRefreshHasWrittenToTheDatabase_ReturnsNull()
    {
        await ARecord(TransientDatabase.DatabaseA, DataRefreshStage.QueuedDonorUpdateProcessing, isOpen: false);
        await ARecord(TransientDatabase.DatabaseB, DataRefreshStage.IndexRemoval, isOpen: true);

        var record = await dataRefreshHistoryRepository.GetLatestRecordThatWroteToDatabase(TransientDatabase.DatabaseB);

        record.Should().BeNull();
    }

    /// <summary>A record of the database, with its stages complete up to and including the given one.</summary>
    /// <param name="isOpen">False for a closed record. It did not succeed, so that it does not change the active database.</param>
    private async Task<DataRefreshRecord> ARecord(TransientDatabase database, DataRefreshStage lastCompletedStage, bool isOpen)
    {
        var record = DataRefreshRecordBuilder.New
            .WithDatabase(database)
            .WithStagesCompletedUpToAndIncluding(lastCompletedStage)
            .Build();
        record.HlaNomenclatureVersion = fixture.Create<string>();
        record.RefreshEndUtc = isOpen ? null : DateTime.UtcNow;

        await dataRefreshHistoryRepository.Create(record);
        return record;
    }
}
