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

    [Test]
    public void Forget_ANotTrackedSet_IsANoOp()
    {
        BuildSut(capacity: 2);

        sut.Forget(1);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void Forget_ATrackedSet_FreesItsSlotWithoutEvictingAnything()
    {
        BuildSut(capacity: 2);
        sut.RecordAccess(1);
        sut.RecordAccess(2);

        // Simulates the cache TTL-expiring set 1's entry independently of any tracker-driven eviction.
        sut.Forget(1);
        // Admitting 3 must not evict 2: forgetting 1 already freed the slot it was occupying.
        sut.RecordAccess(3);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void Forget_AForgottenSetReAccessedLater_IsTrackedAsANewEntry()
    {
        BuildSut(capacity: 1);
        sut.RecordAccess(1);
        sut.Forget(1);

        // 1 was forgotten, so re-accessing it is exactly like admitting a brand new set - it must not immediately
        // evict itself, and re-accessing 2 next now correctly targets 1 as the least-recently-used.
        sut.RecordAccess(1);
        sut.RecordAccess(2);

        evictedSetIds.Should().Equal(1);
    }
}
