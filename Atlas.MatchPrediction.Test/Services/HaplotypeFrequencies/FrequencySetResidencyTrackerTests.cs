using System.Collections.Generic;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetResidencyTrackerTests
{
    private List<int> evictedSetIds;
    private FrequencySetResidencyTracker sut;

    private void BuildSut(int capacity)
    {
        evictedSetIds = [];
        sut = new FrequencySetResidencyTracker(capacity, evictedSetIds.Add);
    }

    [Test]
    public void RecordAccess_WhenUnderCapacity_EvictsNothing()
    {
        BuildSut(capacity: 3);

        sut.RecordAccess(1);
        sut.RecordAccess(2);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordAccess_ReAccessingAnAlreadyTrackedSet_EvictsNothing()
    {
        BuildSut(capacity: 1);

        sut.RecordAccess(1);
        sut.RecordAccess(1);
        sut.RecordAccess(1);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordAccess_WhenANewSetWouldExceedCapacity_EvictsTheLeastRecentlyUsedSet()
    {
        BuildSut(capacity: 1);

        sut.RecordAccess(1);
        sut.RecordAccess(2);

        evictedSetIds.Should().Equal(1);
    }

    [Test]
    public void RecordAccess_ReAccessingASet_ProtectsItFromBeingTheNextEviction()
    {
        BuildSut(capacity: 2);

        sut.RecordAccess(1);
        sut.RecordAccess(2);
        // 1 is now more recently used than 2.
        sut.RecordAccess(1);

        sut.RecordAccess(3);

        evictedSetIds.Should().Equal(2);
    }

    [Test]
    public void RecordAccess_EvictingMultipleSetsInTurn_AlwaysEvictsTheOldestSurvivor()
    {
        BuildSut(capacity: 1);

        sut.RecordAccess(1);
        sut.RecordAccess(2);
        sut.RecordAccess(3);

        evictedSetIds.Should().Equal(1, 2);
    }
}
