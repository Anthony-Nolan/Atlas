using System.Collections.Frozen;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AwesomeAssertions;
using NUnit.Framework;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
public class FrequencyConsolidatorTests
{
    private IFrequencyConsolidator frequencyConsolidator;

    [SetUp]
    public void SetUp()
    {
        frequencyConsolidator = new FrequencyConsolidator();
    }

    [Test]
    public void PreConsolidateFrequenciesForCommonMissingLoci_ConsolidatesAtExpectedLociSets()
    {
        var entry = BuildEntry(
            ("d", "d", "c1", "q1", "d", 1),
            ("d", "d", "c2", "q1", "d", 20),
            ("d", "d", "c1", "q2", "d", 300),
            ("d", "d", "c2", "q2", "d", 4000)
        );

        var consolidated = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);

        // Excluding C merges the two haplotypes that differ only at C.
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.C).Should().Be(21);
        // Excluding DQB1 merges the two haplotypes that differ only at DQB1.
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.Dqb1).Should().Be(301);
        // Excluding both merges all four haplotypes.
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.C, Locus.Dqb1).Should().Be(4321);
        // The other C-excluded group (differing at DQB1) is consolidated independently.
        FrequencyFor(consolidated, entry, "d", "d", "c2", "q2", "d", Locus.C).Should().Be(4300);
    }

    [Test]
    public void PreConsolidateFrequenciesForCommonMissingLoci_DoesNotPreConsolidateUnsupportedExclusions()
    {
        var entry = BuildEntry(
            ("d", "d", "c1", "q1", "d", 1),
            ("d", "d", "c2", "q2", "d", 20)
        );

        var consolidated = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);

        // The supported exclusions (C, DQB1) are pre-calculated...
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.C).Should().Be(1);
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.Dqb1).Should().Be(1);
        // ...but an unsupported exclusion (e.g. B) is never pre-calculated, so it is absent from the collection.
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.B).Should().Be(0);
    }

    [Test]
    public void PreConsolidateFrequenciesForCommonMissingLoci_WithEmptySet_ReturnsEmpty()
    {
        var entry = BuildEntry();

        var consolidated = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);

        consolidated.Should().BeEmpty();
    }

    [Test]
    public void PreConsolidateFrequenciesForCommonMissingLoci_ForHaplotypeWithNoSiblings_KeepsItsOwnFrequency()
    {
        var entry = BuildEntry(("d", "d", "c1", "q1", "d", 7));

        var consolidated = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);

        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.C).Should().Be(7);
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.Dqb1).Should().Be(7);
        FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", Locus.C, Locus.Dqb1).Should().Be(7);
    }

    [TestCaseSource(nameof(CommonExclusions))]
    public void ConsolidateFrequenciesForHaplotype_MatchesThePreConsolidatedValue(Locus[] excludedLoci)
    {
        var entry = BuildEntry(
            ("d", "d", "c1", "q1", "d", 1),
            ("d", "d", "c2", "q1", "d", 20),
            ("d", "d", "c1", "q2", "d", 300),
            ("d", "d", "c2", "q2", "d", 4000)
        );
        var hla = new HaplotypeHla(valueA: "d", valueB: "d", valueC: "c1", valueDqb1: "q1", valueDrb1: "d");

        var consolidated = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);
        var directValue = frequencyConsolidator.ConsolidateFrequenciesForHaplotype(entry, hla, new HashSet<Locus>(excludedLoci));

        // The direct single-value calculation (used while the collection warms) must agree with the warmed collection.
        directValue.Should().Be(FrequencyFor(consolidated, entry, "d", "d", "c1", "q1", "d", excludedLoci));
    }

    [Test]
    public void ConsolidateFrequenciesForHaplotype_WhenHaplotypeAbsentFromSet_ReturnsZero()
    {
        var entry = BuildEntry(("d", "d", "c1", "q1", "d", 1));

        var unrepresented = new HaplotypeHla(valueA: "x", valueB: "x", valueC: "c1", valueDqb1: "q1", valueDrb1: "d");

        frequencyConsolidator.ConsolidateFrequenciesForHaplotype(entry, unrepresented, new HashSet<Locus> { Locus.C })
            .Should().Be(0);
    }

    [Test]
    public void ConsolidateFrequenciesForHaplotype_IgnoresTheValueAtAnExcludedLocus()
    {
        var entry = BuildEntry(
            ("d", "d", "c1", "q1", "d", 1),
            ("d", "d", "c2", "q1", "d", 20)
        );

        // "cX" is unknown to the set, but as C is excluded its value must not affect the result.
        var hla = new HaplotypeHla(valueA: "d", valueB: "d", valueC: "cX", valueDqb1: "q1", valueDrb1: "d");

        frequencyConsolidator.ConsolidateFrequenciesForHaplotype(entry, hla, new HashSet<Locus> { Locus.C })
            .Should().Be(21);
    }

    [Test]
    public void ConsolidateFrequenciesForHaplotype_WhenAlleleAtNonExcludedLocusAbsentFromSet_ReturnsZero()
    {
        var entry = BuildEntry(
            ("d", "d", "c1", "q1", "d", 1),
            ("d", "d", "c2", "q1", "d", 20)
        );

        // "bX" at B (which is NOT excluded) is absent from the set, so nothing can match.
        var hla = new HaplotypeHla(valueA: "d", valueB: "bX", valueC: "c1", valueDqb1: "q1", valueDrb1: "d");

        frequencyConsolidator.ConsolidateFrequenciesForHaplotype(entry, hla, new HashSet<Locus> { Locus.C })
            .Should().Be(0);
    }

    private static readonly Locus[][] CommonExclusions =
    [
        [Locus.C],
        [Locus.Dqb1],
        [Locus.C, Locus.Dqb1]
    ];

    private static FrequencySetCacheEntry BuildEntry(
        params (string A, string B, string C, string Dqb1, string Drb1, decimal Frequency)[] haplotypes)
    {
        var interner = new HaplotypeInterner();
        var dictionary = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();
        foreach (var haplotype in haplotypes)
        {
            var key = interner.Intern(haplotype.A, haplotype.B, haplotype.C, haplotype.Dqb1, haplotype.Drb1);
            dictionary[key] = new HaplotypeFrequencyValue(haplotype.Frequency, default);
        }

        return new FrequencySetCacheEntry
        {
            SetFrequencies = dictionary.ToFrozenDictionary(),
            Interner = interner
        };
    }

    private static decimal FrequencyFor(
        FrozenDictionary<HaplotypeKey, decimal> consolidated,
        FrequencySetCacheEntry entry,
        string a, string b, string c, string dqb1, string drb1,
        params Locus[] excludedLoci)
    {
        var key = entry.Interner.ConvertWherePossible(a, b, c, dqb1, drb1).RemoveLoci(excludedLoci);
        return consolidated.GetValueOrDefault(key);
    }
}
