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

    // The size of one entry, and the keys of three entries.
    private long entrySize;
    private string entryA;
    private string entryB;
    private string entryC;

    private List<string> evictedKeys;
    private FakeLogger<FrequencySetResidencyTracker> logger;
    private FrequencySetResidencyTracker sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        entrySize = fixture.Create<int>();
        var keys = fixture.CreateMany<string>(3).ToList();
        (entryA, entryB, entryC) = (keys[0], keys[1], keys[2]);
    }

    private void BuildSut(long capacityInBytes)
    {
        evictedKeys = [];
        logger = new FakeLogger<FrequencySetResidencyTracker>();
        sut = new FrequencySetResidencyTracker(capacityInBytes, evictedKeys.Add, logger);
    }

    /// <summary>As the cache does: an access, then the size of the entry once it is loaded.</summary>
    /// <returns>The admission of the entry.</returns>
    private long Load(string key, long sizeInBytes)
    {
        sut.RecordAccess(key);
        return sut.RecordSize(key, sizeInBytes);
    }

    [Test]
    public void RecordSize_WhenTheTotalFitsTheCapacity_EvictsNothing()
    {
        BuildSut(capacityInBytes: 2 * entrySize);

        Load(entryA, entrySize);
        Load(entryB, entrySize);

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void RecordAccess_OfANewEntry_EvictsNothing()
    {
        BuildSut(capacityInBytes: entrySize);
        Load(entryA, entrySize);

        // An entry counts from its size, which is known when it is loaded.
        sut.RecordAccess(entryB);

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void RecordSize_WhenTheTotalExceedsTheCapacity_EvictsTheLeastRecentlyUsedEntry()
    {
        BuildSut(capacityInBytes: 2 * entrySize);

        Load(entryA, entrySize);
        Load(entryB, entrySize);
        Load(entryC, entrySize);

        evictedKeys.Should().Equal(entryA);
    }

    [Test]
    public void RecordSize_EvictsTheLeastRecentlyUsedEntriesUntilTheTotalFits()
    {
        BuildSut(capacityInBytes: 2 * entrySize);

        Load(entryA, entrySize);
        Load(entryB, entrySize);
        Load(entryC, 2 * entrySize);

        evictedKeys.Should().Equal(entryA, entryB);
    }

    [Test]
    public void RecordSize_ReAccessingAnEntry_ProtectsItFromBeingTheNextEviction()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        Load(entryA, entrySize);
        Load(entryB, entrySize);

        // A is now more recently used than B.
        sut.RecordAccess(entryA);
        Load(entryC, entrySize);

        evictedKeys.Should().Equal(entryB);
    }

    [Test]
    public void RecordSize_OfAnEntryLargerThanTheCapacity_EvictsTheOtherEntries_AndKeepsThatEntry()
    {
        BuildSut(capacityInBytes: entrySize);
        Load(entryA, entrySize);

        // Not evicted at once: each access would then load it again.
        Load(entryB, 2 * entrySize);

        evictedKeys.Should().Equal(entryA);
    }

    [Test]
    public void RecordSize_AfterAnEntryLargerThanTheCapacity_EvictsThatEntry()
    {
        BuildSut(capacityInBytes: entrySize);
        Load(entryA, 2 * entrySize);

        Load(entryB, entrySize);

        evictedKeys.Should().Equal(entryA);
    }

    [Test]
    public void RecordSize_OfAnEntryLargerThanTheCapacity_LogsAWarning()
    {
        BuildSut(capacityInBytes: entrySize);

        Load(entryA, 2 * entrySize);

        logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Test]
    public void RecordSize_DoesNotEvictAnEntryThatIsStillLoading()
    {
        BuildSut(capacityInBytes: 2 * entrySize);

        // B is the least recently used, but holds no bytes yet: its removal would only make its callers load it again.
        sut.RecordAccess(entryB);
        Load(entryA, entrySize);
        Load(entryC, 2 * entrySize);

        evictedKeys.Should().Equal(entryA);
    }

    [Test]
    public void RecordSize_OfAnEntryLoadedAgain_ReplacesItsSize()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        Load(entryA, entrySize);

        // A loaded again, e.g. after an expiry whose Forget has not run yet: it counts once, not twice.
        Load(entryA, entrySize);
        Load(entryB, entrySize);

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void RecordLoadStart_OfALoadedEntry_FreesItsBytes()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        Load(entryA, entrySize);
        Load(entryB, entrySize);

        // A expired, and loads again before the Forget of its earlier entry ran.
        sut.RecordLoadStart(entryA);
        Load(entryC, entrySize);

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void RecordSize_DoesNotEvictAnEntryThatIsLoadingAgain()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        Load(entryA, entrySize);
        Load(entryB, entrySize);

        // A loads again, and B is used after that: A is the least recently used entry, but the entry of A in the cache is
        // the one that loads, and no callback could forget its bytes.
        sut.RecordAccess(entryA);
        sut.RecordLoadStart(entryA);
        sut.RecordAccess(entryB);
        Load(entryC, 2 * entrySize);

        evictedKeys.Should().Equal(entryB);
    }

    [Test]
    public void Forget_ANotTrackedEntry_IsANoOp()
    {
        BuildSut(capacityInBytes: 2 * entrySize);

        sut.Forget(entryA, fixture.Create<long>());

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void Forget_FreesTheBytesOfTheEntry()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        var admissionOfA = Load(entryA, entrySize);
        Load(entryB, entrySize);

        // As when the cache entry of A expires.
        sut.Forget(entryA, admissionOfA);
        Load(entryC, entrySize);

        evictedKeys.Should().BeEmpty();
    }

    [Test]
    public void Forget_WithTheAdmissionOfAnEarlierEntry_DoesNotForgetTheEntryThatReplacedIt()
    {
        BuildSut(capacityInBytes: 2 * entrySize);
        var admissionOfTheFirstEntry = Load(entryA, entrySize);
        Load(entryA, entrySize);

        // The post-eviction callback of the first entry, late on the thread pool.
        sut.Forget(entryA, admissionOfTheFirstEntry);
        Load(entryB, entrySize);
        Load(entryC, entrySize);

        // A is still tracked: so it is the least recently used entry, and goes first.
        evictedKeys.Should().Equal(entryA);
    }

    [Test]
    public void Forget_AfterTheTrackerEvictedTheEntry_DoesNotFreeItsBytesTwice()
    {
        BuildSut(capacityInBytes: entrySize);
        var admissionOfA = Load(entryA, entrySize);
        Load(entryB, entrySize);

        // The post-eviction callback of A, after the tracker evicted A.
        sut.Forget(entryA, admissionOfA);
        Load(entryC, entrySize);

        evictedKeys.Should().Equal(entryA, entryB);
    }
}
