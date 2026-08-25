using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.CompressedPhenotypeExpansion;

/// <summary>
/// ATL-233 T4's acceptance criterion 1: the integer mask keeps <b>exactly</b> the pairs the string predicate kept.
///
/// <para>
/// The expander no longer asks "is this pair representable?" per pair; it asks "which positions can this haplotype
/// occupy?" per survivor and then ANDs two masks. Those are the same question only because the shipped predicate never
/// read the other haplotype of the pair - a claim worth an assertion rather than a comment, because a wrong bit lane
/// or a wrong shift silently changes which genotypes a donor gets.
/// </para>
///
/// <para>
/// So the expected set here is computed by brute force, in the test, in the shipped predicate's own shape: for every
/// unordered pair, at every allowed locus, haplotype 1 at position 1 with haplotype 2 at position 2, or the reverse.
/// The fixture is built so that the answer is neither "all pairs" nor "no pairs" - a mask that was always full or
/// always empty would pass a weaker test - and so that it covers the cases most likely to break: a locus represented
/// at one position only, a locus whose positions overlap (so a homozygous self-pair survives), and a locus the subject
/// is untyped at (so the predicate short circuits to true on a null group set).
/// </para>
/// </summary>
[TestFixture]
[NonParallelizable]
internal class PairRepresentationMaskTests
{
    private const int FrequencySetId = 11;

    /// <summary>A, B and DRB1 discriminate; DQB1 is allowed but untyped, which is the null-groups branch.</summary>
    private static readonly Locus[] AllowedLoci = [Locus.A, Locus.B, Locus.Drb1, Locus.Dqb1];

    private Fixture fixture;
    private ICompressedPhenotypeConverter converter;
    private IHaplotypeFrequencyService haplotypeFrequencyService;
    private CompressedPhenotypeExpander sut;

    private string[] aAlleles;
    private string[] bAlleles;
    private string[] drb1Alleles;
    private string dqb1Allele;

    // Position 1 and position 2 group sets per discriminating locus, as the subject's phenotype holds them.
    private Dictionary<Locus, (ISet<string> Position1, ISet<string> Position2)> groupsByLocus;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        converter = Substitute.For<ICompressedPhenotypeConverter>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();

        aAlleles = fixture.CreateMany<string>(3).ToArray();
        bAlleles = fixture.CreateMany<string>(3).ToArray();
        drb1Alleles = fixture.CreateMany<string>(2).ToArray();
        dqb1Allele = fixture.Create<string>();

        groupsByLocus = new Dictionary<Locus, (ISet<string>, ISet<string>)>
        {
            // Overlap at index 1, so a haplotype carrying it can occupy either position - the homozygous case.
            [Locus.A] = (new HashSet<string> { aAlleles[0], aAlleles[1] }, new HashSet<string> { aAlleles[1], aAlleles[2] }),
            [Locus.B] = (new HashSet<string> { bAlleles[0], bAlleles[1] }, new HashSet<string> { bAlleles[1], bAlleles[2] }),

            // Position 2 admits one allele only, so drb1Alleles[1] can never sit there - this is what makes the
            // direct/inverted distinction bite rather than being symmetric.
            [Locus.Drb1] = (new HashSet<string> { drb1Alleles[0], drb1Alleles[1] }, new HashSet<string> { drb1Alleles[0] })
        };

        converter.ConvertPhenotype(null).ReturnsForAnyArgs(SubjectGroups());
        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(Pool());

        sut = new CompressedPhenotypeExpander(converter, haplotypeFrequencyService);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_KeepsExactlyThePairsTheStringPredicateKept()
    {
        var expanded = await sut.ExpandCompressedPhenotype(Input());

        var actual = expanded.GenotypeHlaNames.Select(UnorderedPairKey).ToList();
        var expected = ExpectedPairsByBruteForce();

        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WithADiscriminatingPhenotype_KeepsSomePairsAndRejectsOthers()
    {
        // Guards the test above from passing vacuously: a mask stuck at "all bits set" or "no bits set" would agree
        // with a brute force that had the same bug, but it cannot also land strictly between 0 and every pair.
        var survivors = SurvivingHaplotypes().Count;
        var examinedPairs = survivors * (survivors + 1) / 2;

        var expanded = await sut.ExpandCompressedPhenotype(Input());

        expanded.GenotypeHlaNames.Should().NotBeEmpty();
        expanded.GenotypeHlaNames.Count.Should().BeLessThan(examinedPairs);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_KeepsAHomozygousPairWhenBothPositionsAdmitTheHaplotype()
    {
        // The self-pair the loop's inner index deliberately includes: every locus's overlapping allele, so the same
        // haplotype is representable at position 1 and position 2 at once.
        var homozygous = Haplotype(aAlleles[1], bAlleles[1], drb1Alleles[0]);

        var expanded = await sut.ExpandCompressedPhenotype(Input());

        expanded.GenotypeHlaNames.Select(UnorderedPairKey).Should().Contain(UnorderedPairKey(homozygous, homozygous));
    }

    [Test]
    public async Task ExpandCompressedPhenotype_PairsOverExactlyTheFilteredPool()
    {
        // The mask is a property of a survivor alone, so it must change neither the pool the pairing runs over nor how
        // many pairs come out of it. Both are asserted against a brute force that does not use the mask at all.
        var survivors = SurvivingHaplotypes().Count;

        var expanded = await sut.ExpandCompressedPhenotype(Input());

        expanded.Haplotypes.Count.Should().Be(survivors);
        expanded.GenotypeCount.Should().Be(ExpectedPairsByBruteForce().Count);
    }

    /// <summary>
    /// The shipped predicate, written out: at every allowed locus, one haplotype at position 1 and the other at
    /// position 2, in either order. An untyped locus (no group set) is represented by anything.
    /// </summary>
    private bool IsRepresentedPair(string[] haplotype1, string[] haplotype2)
    {
        return AllowedLoci.All(locus =>
        {
            if (!groupsByLocus.TryGetValue(locus, out var groups))
            {
                return true;
            }

            var hla1 = AlleleAt(haplotype1, locus);
            var hla2 = AlleleAt(haplotype2, locus);

            return (groups.Position1.Contains(hla1) && groups.Position2.Contains(hla2)) ||
                   (groups.Position2.Contains(hla1) && groups.Position1.Contains(hla2));
        });
    }

    private List<string> ExpectedPairsByBruteForce()
    {
        var survivors = SurvivingHaplotypes();
        var expected = new List<string>();

        for (var i = 0; i < survivors.Count; i++)
        {
            for (var j = i; j < survivors.Count; j++)
            {
                if (IsRepresentedPair(survivors[i], survivors[j]))
                {
                    expected.Add(UnorderedPairKey(survivors[i], survivors[j]));
                }
            }
        }

        return expected;
    }

    /// <summary>
    /// Every haplotype the pool filter keeps: the subject explains an allele at a locus when either position's group
    /// set holds it, so the filter's reach is the union of the two positions.
    /// </summary>
    private List<string[]> SurvivingHaplotypes()
    {
        return (from a in aAlleles
            from b in bAlleles
            from drb1 in drb1Alleles
            select Haplotype(a, b, drb1)).ToList();
    }

    private string[] Haplotype(string a, string b, string drb1) => [a, b, drb1, dqb1Allele];

    private static string AlleleAt(string[] haplotype, Locus locus) => haplotype[Array.IndexOf(AllowedLoci, locus)];

    /// <summary>
    /// A genotype's two haplotypes, in an order the survivor list's own (hash set) ordering cannot affect. The pair
    /// test is symmetric - swapping the haplotypes swaps "direct" with "inverted" - so the pair is the unit of
    /// comparison, not the (position 1, position 2) assignment within it.
    /// </summary>
    private static string UnorderedPairKey(string[] haplotype1, string[] haplotype2)
    {
        var first = string.Join("~", haplotype1);
        var second = string.Join("~", haplotype2);

        return string.CompareOrdinal(first, second) <= 0 ? $"{first}|{second}" : $"{second}|{first}";
    }

    private string UnorderedPairKey(PhenotypeInfo<string> genotype)
    {
        var position1 = AllowedLoci.Select(l => genotype.GetPosition(l, LocusPosition.One)).ToArray();
        var position2 = AllowedLoci.Select(l => genotype.GetPosition(l, LocusPosition.Two)).ToArray();

        return UnorderedPairKey(position1, position2);
    }

    private CompressedPhenotypeExpanderInput Input() => new()
    {
        Phenotype = new PhenotypeInfo<string>(),
        HfSetId = FrequencySetId,
        HfSetHlaNomenclatureVersion = fixture.Create<string>(),
        MatchPredictionParameters = new MatchPredictionParameters(AllowedLoci.ToHashSet())
    };

    private DataByResolution<PhenotypeInfo<ISet<string>>> SubjectGroups() =>
        new()
        {
            GGroup = new PhenotypeInfo<ISet<string>>(),
            PGroup = new PhenotypeInfo<ISet<string>>(),

            // DQB1 is left unset on purpose: allowed, but with no group set, which is the branch where
            // IsRepresentedInTargetPhenotype returns true without probing anything.
            SmallGGroup = new PhenotypeInfo<ISet<string>>()
                .SetLocus(Locus.A, groupsByLocus[Locus.A].Position1, groupsByLocus[Locus.A].Position2)
                .SetLocus(Locus.B, groupsByLocus[Locus.B].Position1, groupsByLocus[Locus.B].Position2)
                .SetLocus(Locus.Drb1, groupsByLocus[Locus.Drb1].Position1, groupsByLocus[Locus.Drb1].Position2)
        };

    /// <summary>
    /// Every combination the subject can explain, plus one haplotype it cannot - so the filter has something to drop
    /// and S is smaller than H, as it is for a real donor.
    /// </summary>
    private FrequencySetCacheEntry Pool()
    {
        var interner = new HaplotypeInterner();
        var frequencies = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();

        void Add(string a, string b, string drb1) =>
            frequencies.Add(
                interner.Intern(a, b, c: null, dqb1: dqb1Allele, drb1: drb1),
                new HaplotypeFrequencyValue(fixture.Create<decimal>(), HaplotypeTypingCategory.SmallGGroup));

        foreach (var haplotype in SurvivingHaplotypes())
        {
            Add(haplotype[0], haplotype[1], haplotype[2]);
        }

        Add(fixture.Create<string>(), bAlleles[0], drb1Alleles[0]);

        return new FrequencySetCacheEntry
        {
            SetFrequencies = frequencies.ToFrozenDictionary(),
            Interner = interner
        };
    }
}
