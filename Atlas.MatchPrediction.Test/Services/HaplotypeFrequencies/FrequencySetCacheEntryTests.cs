using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

/// <summary>
/// <see cref="FrequencySetCacheEntry.AlleleIndex"/> is what lets <c>CompressedPhenotypeExpander</c> seed its
/// survivor scan from a handful of candidates instead of the whole pool, so its correctness is exactly: does
/// <c>AlleleIndex[category].GetLocus(locus)[id]</c> hold precisely the <see cref="FrequencySetCacheEntry.ProjectedPool"/>
/// positions whose haplotype carries <c>id</c> at <c>locus</c> - no more, no fewer, and in ascending pool order.
/// </summary>
[TestFixture]
internal class FrequencySetCacheEntryTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void AlleleIndex_BucketsMatchABruteForceScanOfTheProjectedPool()
    {
        var entry = Entry(
            (a: "a1", b: "b1"),
            (a: "a2", b: "b1"),
            (a: "a1", b: "b2"),
            (a: "a2", b: "b2"),
            (a: "a1", b: "b1"));

        var pool = entry.ProjectedPool.SmallGGroup;
        var index = entry.AlleleIndex.SmallGGroup;

        foreach (var locus in new[] { Locus.A, Locus.B })
        {
            var buckets = index.GetLocus(locus);

            for (var id = 0; id < buckets.Length; id++)
            {
                var expected = Enumerable.Range(0, pool.Length).Where(i => pool[i].GetLocus(locus) == id).ToArray();
                buckets[id].Should().Equal(expected);
            }
        }
    }

    [Test]
    public void AlleleIndex_AccessedTwice_IsBuiltOnce()
    {
        var entry = Entry((a: "a1", b: "b1"), (a: "a2", b: "b1"));

        var first = entry.AlleleIndex;
        var second = entry.AlleleIndex;

        // Reference identity, not equality: equality would pass just as happily against an index rebuilt on every
        // access, which is the thing this rules out - CompressedPhenotypeExpander relies on this being paid once
        // per set, not once per donor, exactly as it already does for ProjectedPool.
        second.Should().BeSameAs(first);
        second.SmallGGroup.Should().BeSameAs(first.SmallGGroup);
    }

    private FrequencySetCacheEntry Entry(params (string a, string b)[] haplotypes)
    {
        var interner = new HaplotypeInterner();
        var frequencies = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();

        foreach (var (a, b) in haplotypes)
        {
            var key = interner.Intern(a, b, c: null, dqb1: null, drb1: null);

            // Distinct haplotypes only - the fixture above intentionally repeats (a1, b1), so skip re-adding it and
            // let the index reflect one pool entry standing for that duplicate, same as the real interner would.
            frequencies.TryAdd(key, new HaplotypeFrequencyValue(fixture.Create<decimal>(), HaplotypeTypingCategory.SmallGGroup));
        }

        return new FrequencySetCacheEntry
        {
            SetFrequencies = frequencies.ToFrozenDictionary(),
            Interner = interner
        };
    }
}
