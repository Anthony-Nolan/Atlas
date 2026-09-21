using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using Atlas.MatchPrediction.Test.TestHelpers;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.CompressedPhenotypeExpansion;

/// <summary>
/// The pool filter tests an allele by a lookup in a <c>bool[]</c> indexed by interned allele id, rather than by
/// <c>ISet&lt;string&gt;.Contains</c> on its name. The pass/fail answer must be the same either way, and there are
/// exactly three places it could differ: an allele the <b>set</b> has never seen (which resolves to
/// <c>NotFound</c>, not to an id), a
/// haplotype <b>untyped</b> at an allowed locus (whose id is 0, the same id an empty allele name interns to), and a
/// subject <b>untyped</b> at an allowed locus (whose group set is null, and therefore admits everything).
///
/// <para>
/// These are asserted through the real expander over a real interned set, because the id space only exists inside a
/// <see cref="FrequencySetCacheEntry"/> - testing the filter in isolation would have to fabricate that space and
/// would prove nothing about the one production builds.
/// </para>
/// </summary>
[TestFixture]
[NonParallelizable]
internal class PoolFilterAlleleIdTests
{
    private const int FrequencySetId = 23;

    private Fixture fixture;
    private ICompressedPhenotypeConverter converter;
    private IHaplotypeFrequencyService haplotypeFrequencyService;
    private CompressedPhenotypeExpander sut;

    private string a1;
    private string a2;
    private string b1;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        converter = Substitute.For<ICompressedPhenotypeConverter>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();

        a1 = fixture.Create<string>();
        a2 = fixture.Create<string>();
        b1 = fixture.Create<string>();

        sut = new CompressedPhenotypeExpander(converter, haplotypeFrequencyService);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenASubjectGroupIsAbsentFromTheSet_MatchesNothingWithIt()
    {
        // The allele resolves to AlleleInterner.NotFound rather than to an id, so it must mark no id as admissible -
        // an id-indexed mask that used the sentinel would index out of bounds or, worse, admit id 0.
        var absentFromSet = fixture.Create<string>();

        var survivors = await Survivors(
            subjectA: [a1, absentFromSet],
            pool: [(a1, b1), (a2, b1)],
            subjectB: [b1]);

        survivors.Should().Be(1);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenAPooledHaplotypeIsUntypedAtAnAllowedLocus_DropsIt()
    {
        // The untyped locus interns to id 0. A subject that names real alleles there does not admit id 0, which is the
        // same answer a Contains(haplotype allele) gives against a null.
        var survivors = await Survivors(
            subjectA: [a1, a2],
            pool: [(a1, b1), (a2, b1), (null, b1)],
            subjectB: [b1]);

        survivors.Should().Be(2);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSubjectIsUntypedAtAnAllowedLocus_AdmitsEveryHaplotypeThere()
    {
        // No group set at B, so the mask is null and B constrains nothing - the `hlaGroups == null` branch.
        // A stays ambiguous so the expansion still takes the pooled path rather than the unambiguous short circuit.
        var b2 = fixture.Create<string>();

        var survivors = await Survivors(
            subjectA: [a1, a2],
            pool: [(a1, b1), (a2, b2)],
            subjectB: null);

        survivors.Should().Be(2);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSubjectIsUntypedAtEveryAllowedLocus_FallsBackToTheWholePool()
    {
        // Every allowed locus's mask is null, so no locus can seed a restricted candidate set from the allele index -
        // SelectCandidatePositions has nothing to narrow with and falls back to the full pool, in pool order, exactly
        // as the loop always did before the index existed.
        var survivors = await Survivors(
            subjectA: null,
            pool: [(a1, b1), (a2, b1)],
            subjectB: null);

        survivors.Should().Be(2);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSubjectsGroupResolvesToMultipleAllowedIds_MergesTheirIndexBuckets()
    {
        // Both a1 and a2 are admitted at A - an ambiguous/MAC-expanded typing group - so the seed locus's admitted
        // ids span more than one index bucket, and SelectCandidatePositions must merge them (in pool order) rather
        // than reading a single bucket straight through.
        var a3 = fixture.Create<string>();

        var survivors = await Survivors(
            subjectA: [a1, a2],
            pool: [(a1, b1), (a3, b1), (a2, b1)],
            subjectB: [b1]);

        survivors.Should().Be(2);
    }

    /// <summary>
    /// Survivor count (S) for a subject and pool - the size of the pool the pairing loop is quadratic in. A null
    /// <paramref name="subjectB"/> means untyped at B - passed explicitly by every caller, because defaulting it would
    /// make "untyped" indistinguishable from "not mentioned", which is the very distinction two of these tests turn on.
    /// </summary>
    private async Task<int> Survivors(string[] subjectA, (string A, string B)[] pool, string[] subjectB)
    {
        converter.StubGroups(SubjectGroups(subjectA, subjectB));
        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(Pool(pool));

        var expanded = await sut.ExpandCompressedPhenotype(new CompressedPhenotypeExpanderInput
        {
            Phenotype = new PhenotypeInfo<string>(),
            HfSetId = FrequencySetId,
            HfSetHlaNomenclatureVersion = fixture.Create<string>(),
            MatchPredictionParameters = new MatchPredictionParameters(new HashSet<Locus> { Locus.A, Locus.B })
        });

        return expanded.Haplotypes.Count;
    }

    private static DataByResolution<PhenotypeInfo<ISet<string>>> SubjectGroups(string[] atA, string[] atB)
    {
        var smallGGroup = new PhenotypeInfo<ISet<string>>();

        if (atA != null)
        {
            smallGGroup = smallGGroup.SetLocus(Locus.A, new HashSet<string>(atA));
        }

        if (atB != null)
        {
            smallGGroup = smallGGroup.SetLocus(Locus.B, new HashSet<string>(atB));
        }

        return new DataByResolution<PhenotypeInfo<ISet<string>>>
        {
            GGroup = new PhenotypeInfo<ISet<string>>(),
            PGroup = new PhenotypeInfo<ISet<string>>(),
            SmallGGroup = smallGGroup
        };
    }

    private FrequencySetCacheEntry Pool((string A, string B)[] haplotypes)
    {
        var interner = new HaplotypeInterner();
        var frequencies = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();

        foreach (var (a, b) in haplotypes)
        {
            frequencies.Add(
                interner.Intern(a, b, c: null, dqb1: null, drb1: null),
                new HaplotypeFrequencyValue(fixture.Create<decimal>(), HaplotypeTypingCategory.SmallGGroup));
        }

        return new FrequencySetCacheEntry
        {
            SetFrequencies = frequencies.ToFrozenDictionary(),
            Interner = interner
        };
    }
}
