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
    private long Load(int setId, long sizeInBytes)
    {
        sut.RecordAccess(setId);
        return sut.RecordSize(setId, sizeInBytes);
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
}
