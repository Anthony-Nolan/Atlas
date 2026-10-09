using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

[TestFixture]
internal class DonorGenotypePrecomputationTargetReaderTests
{
    private Fixture fixture = null!;
    private Dictionary<int, TransientDatabase> openRecordDatabases = null!;
    private IDataRefreshHistoryRepository dataRefreshHistoryRepository = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        openRecordDatabases = [];
        dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        dataRefreshHistoryRepository.GetIncompleteRefreshJobDatabases().Returns(_ => openRecordDatabases);
    }

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public async Task ReadDatabase_WhenTheRecordIsOpen_ReturnsItsDatabase(TransientDatabase database)
    {
        var recordId = GivenAnOpenRecord(database);

        var target = await DonorGenotypePrecomputationTargetReader.ReadDatabase(dataRefreshHistoryRepository, recordId);

        target.Should().Be(database);
    }

    [Test]
    public async Task ReadDatabase_WhenNoRecordIsOpen_ReturnsNull()
    {
        // Outside a data refresh: a message that waited from a refresh that has ended.
        var target = await DonorGenotypePrecomputationTargetReader.ReadDatabase(dataRefreshHistoryRepository, fixture.Create<int>());

        target.Should().BeNull();
    }

    [Test]
    public async Task ReadDatabase_WhenTheRecordIsClosedOrDoesNotExist_AndAnotherRecordIsOpen_ReturnsNull()
    {
        // The repository returns only the open records. A message of an earlier refresh must not write to the database of
        // the refresh that runs now.
        GivenAnOpenRecord(fixture.Create<TransientDatabase>());

        var target = await DonorGenotypePrecomputationTargetReader.ReadDatabase(dataRefreshHistoryRepository, fixture.Create<int>());

        target.Should().BeNull();
    }

    [Test]
    public async Task ReadDatabase_WhenMoreThanOneRecordIsOpen_ReturnsTheDatabaseOfTheRecordOfTheMessage()
    {
        // The message names its record, so the other open records do not matter.
        var database = fixture.Create<TransientDatabase>();
        GivenAnOpenRecord(database.Other());
        var recordId = GivenAnOpenRecord(database);

        var target = await DonorGenotypePrecomputationTargetReader.ReadDatabase(dataRefreshHistoryRepository, recordId);

        target.Should().Be(database);
    }

    private int GivenAnOpenRecord(TransientDatabase database)
    {
        var recordId = fixture.Create<int>();
        openRecordDatabases.Add(recordId, database);
        return recordId;
    }
}
