using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

[TestFixture]
internal class DonorGenotypePrecomputationTargetReaderTests
{
    private Fixture fixture = null!;
    private List<DataRefreshRecord> openRecords = null!;
    private IDataRefreshHistoryRepository dataRefreshHistoryRepository = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        openRecords = [];
        dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(_ => openRecords);
    }

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public void Read_ReturnsTheOpenDataRefreshRecord_AndTheDatabaseThatItFills(TransientDatabase database)
    {
        var record = GivenAnOpenRecord(database);

        var target = DonorGenotypePrecomputationTargetReader.Read(dataRefreshHistoryRepository);

        target.Should().Be(new DonorGenotypePrecomputationTarget(record.Id, database));
    }

    [Test]
    public void Read_WhenNoDataRefreshRecordIsOpen_Throws()
    {
        // The stage sends messages only while its refresh is open, so the worker has no database to write.
        var act = () => DonorGenotypePrecomputationTargetReader.Read(dataRefreshHistoryRepository);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Read_WhenMoreThanOneDataRefreshRecordIsOpen_ThrowsWithTheirIds()
    {
        // The requester starts no refresh while another is open. The worker does not guess which record is the real one.
        var records = new[] { GivenAnOpenRecord(TransientDatabase.DatabaseA), GivenAnOpenRecord(TransientDatabase.DatabaseB) };

        var act = () => DonorGenotypePrecomputationTargetReader.Read(dataRefreshHistoryRepository);

        act.Should().Throw<InvalidOperationException>().Which.Message.Should().ContainAll(records.Select(record => record.Id.ToString()));
    }

    private DataRefreshRecord GivenAnOpenRecord(TransientDatabase database)
    {
        var record = fixture.Build<DataRefreshRecord>().With(r => r.Database, database.ToString()).Create();
        openRecords.Add(record);
        return record;
    }
}
