using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetResidencyTrackerTests
{
    private Fixture fixture;

    // The size of one set, and three set ids.
    private long setSize;
    private int setA;
    private int setB;
    private int setC;

    private List<int> evictedSetIds;
    private FakeLogger<FrequencySetResidencyTracker> logger;
    private FrequencySetResidencyTracker sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        setSize = fixture.Create<int>();
        var setIds = fixture.CreateMany<int>(3).ToList();
        (setA, setB, setC) = (setIds[0], setIds[1], setIds[2]);
    }

    private void BuildSut(long capacityInBytes)
    {
        evictedSetIds = [];
        logger = new FakeLogger<FrequencySetResidencyTracker>();
        sut = new FrequencySetResidencyTracker(capacityInBytes, evictedSetIds.Add, logger);
    }

    /// <summary>As the cache does: an access, then the size of the set once it is loaded.</summary>
    /// <returns>The admission of the set.</returns>
    private long Load(int setId, long sizeInBytes, DateTimeOffset? importTime = null)
    {
        sut.RecordAccess(setId);
        return sut.RecordSize(setId, sizeInBytes, importTime);
    }

    [Test]
    public void RecordSize_WhenTheTotalFitsTheCapacity_EvictsNothing()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        Load(setA, setSize);
        Load(setB, setSize);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordAccess_OfANewSet_EvictsNothing()
    {
        BuildSut(capacityInBytes: setSize);
        Load(setA, setSize);

        // A set counts from its size, which is known when it is loaded.
        sut.RecordAccess(setB);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordSize_WhenTheTotalExceedsTheCapacity_EvictsTheLeastRecentlyUsedSet()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        Load(setA, setSize);
        Load(setB, setSize);
        Load(setC, setSize);

        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void RecordSize_EvictsTheLeastRecentlyUsedSetsUntilTheTotalFits()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        Load(setA, setSize);
        Load(setB, setSize);
        Load(setC, 2 * setSize);

        evictedSetIds.Should().Equal(setA, setB);
    }

    [Test]
    public void RecordSize_ReAccessingASet_ProtectsItFromBeingTheNextEviction()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize);
        Load(setB, setSize);

        // A is now more recently used than B.
        sut.RecordAccess(setA);
        Load(setC, setSize);

        evictedSetIds.Should().Equal(setB);
    }

    [Test]
    public void RecordSize_OfASetLargerThanTheCapacity_EvictsTheOtherSets_AndKeepsThatSet()
    {
        BuildSut(capacityInBytes: setSize);
        Load(setA, setSize);

        // Not evicted at once: each access would then load it again.
        Load(setB, 2 * setSize);

        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void RecordSize_AfterASetLargerThanTheCapacity_EvictsThatSet()
    {
        BuildSut(capacityInBytes: setSize);
        Load(setA, 2 * setSize);

        Load(setB, setSize);

        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void RecordSize_OfASetLargerThanTheCapacity_LogsAWarning()
    {
        BuildSut(capacityInBytes: setSize);

        Load(setA, 2 * setSize);

        logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Test]
    public void RecordSize_DoesNotEvictASetThatIsStillLoading()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        // B is the least recently used, but holds no bytes yet: its removal would only make its callers load it again.
        sut.RecordAccess(setB);
        Load(setA, setSize);
        Load(setC, 2 * setSize);

        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void RecordSize_OfASetLoadedAgain_ReplacesItsSize()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize);

        // A loaded again, e.g. after an expiry whose Forget has not run yet: it counts once, not twice.
        Load(setA, setSize);
        Load(setB, setSize);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordLoadStart_OfALoadedSet_FreesItsBytes()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize);
        Load(setB, setSize);

        // A expired, and loads again before the Forget of its earlier entry ran.
        sut.RecordLoadStart(setA);
        Load(setC, setSize);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void RecordSize_DoesNotEvictASetThatIsLoadingAgain()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize);
        Load(setB, setSize);

        // A loads again, and B is used after that: A is the least recently used set, but the entry of A in the cache is
        // the one that loads, and no callback could forget its bytes.
        sut.RecordAccess(setA);
        sut.RecordLoadStart(setA);
        sut.RecordAccess(setB);
        Load(setC, 2 * setSize);

        evictedSetIds.Should().Equal(setB);
    }

    [Test]
    public void Forget_ANotTrackedSet_IsANoOp()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        sut.Forget(setA, fixture.Create<long>());

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void Forget_FreesTheBytesOfTheSet()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        var admissionOfA = Load(setA, setSize);
        Load(setB, setSize);

        // As when the cache entry of A expires.
        sut.Forget(setA, admissionOfA);
        Load(setC, setSize);

        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void Forget_WithTheAdmissionOfAnEarlierEntry_DoesNotForgetTheEntryThatReplacedIt()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        var admissionOfTheFirstEntry = Load(setA, setSize);
        Load(setA, setSize);

        // The post-eviction callback of the first entry, late on the thread pool.
        sut.Forget(setA, admissionOfTheFirstEntry);
        Load(setB, setSize);
        Load(setC, setSize);

        // A is still tracked: so it is the least recently used set, and goes first.
        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void Forget_AfterTheTrackerEvictedTheSet_DoesNotFreeItsBytesTwice()
    {
        BuildSut(capacityInBytes: setSize);
        var admissionOfA = Load(setA, setSize);
        Load(setB, setSize);

        // The post-eviction callback of A, after the tracker evicted A.
        sut.Forget(setA, admissionOfA);
        Load(setC, setSize);

        evictedSetIds.Should().Equal(setA, setB);
    }

    [Test]
    public void EvictIfReplaced_WithTheImportTimeOfTheLoadedEntry_EvictsNothing()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        var importTime = fixture.Create<DateTimeOffset>();
        Load(setA, setSize, importTime);

        var evicted = sut.EvictIfReplaced(setA, importTime);

        evicted.Should().BeFalse();
        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void EvictIfReplaced_WithAnotherImportTime_EvictsTheSet_AndFreesItsBytes()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize, fixture.Create<DateTimeOffset>());
        Load(setB, setSize);

        var evicted = sut.EvictIfReplaced(setA, fixture.Create<DateTimeOffset>());
        Load(setC, setSize);

        evicted.Should().BeTrue();
        // C takes the bytes of A: B stays.
        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void EvictIfReplaced_WhenTheSetHasNoRow_EvictsTheSet()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize, fixture.Create<DateTimeOffset>());

        var evicted = sut.EvictIfReplaced(setA, null);

        evicted.Should().BeTrue();
        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void EvictIfReplaced_OfASetLoadedWithNoRow_WhenTheRowExists_EvictsTheSet()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        // A set with no row, and so no import time, has no frequencies either: its entry holds no bytes.
        Load(setA, 0);

        var evicted = sut.EvictIfReplaced(setA, fixture.Create<DateTimeOffset>());

        evicted.Should().BeTrue();
        evictedSetIds.Should().Equal(setA);
    }

    [Test]
    public void EvictIfReplaced_OfASetThatIsLoading_EvictsNothing()
    {
        BuildSut(capacityInBytes: 2 * setSize);
        Load(setA, setSize, fixture.Create<DateTimeOffset>());

        // A loads again: that load reads the current row.
        sut.RecordLoadStart(setA);
        var evicted = sut.EvictIfReplaced(setA, fixture.Create<DateTimeOffset>());

        evicted.Should().BeFalse();
        evictedSetIds.Should().BeEmpty();
    }

    [Test]
    public void EvictIfReplaced_OfASetThatIsNotTracked_EvictsNothing()
    {
        BuildSut(capacityInBytes: 2 * setSize);

        var evicted = sut.EvictIfReplaced(setA, fixture.Create<DateTimeOffset>());

        evicted.Should().BeFalse();
        evictedSetIds.Should().BeEmpty();
    }
}
