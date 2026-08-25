using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AutoFixture;
using AwesomeAssertions;
using Atlas.MatchPrediction.Test.TestHelpers;
using NSubstitute;
using NUnit.Framework;
using HfSet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

/// <summary>
/// The equivalence guard ATL-233 Annex B rule 1 requires, for T1 (cache the projected haplotype pool) and T2 (stop
/// resolving a frequency per genotype).
///
/// <para>
/// It asserts at the <see cref="IGenotypeImputationService.Impute"/> boundary, whose signature neither ticket
/// changes, and it asserts <b>exact</b> <c>decimal</c> likelihoods worked out by hand rather than snapshotted. So it
/// is an oracle, not a record of whatever the code happened to do: it passes against the shipped implementation and
/// must still pass afterwards.
/// </para>
///
/// <para>
/// <b>Read <see cref="Impute_OnAReducedKey_UsesTheConsolidatedFrequencyForTheCollapsedSurvivor"/> first.</b> T2 as
/// originally written in the ticket pack proposed carrying each pooled haplotype's own <c>SetFrequencies</c> value
/// and multiplying. On any key with excluded loci - 74.0% of the precompute's rows - a survivor is nulled at those
/// loci (<c>CompressedPhenotypeExpander</c>, the <c>allowedLoci.Contains(l) ? hla : null</c> map) and therefore
/// stands for a <i>group</i> of stored haplotypes whose frequencies are summed. That test fails for any
/// implementation that carries an individual frequency instead of asking
/// <see cref="IHaplotypeFrequencyService.GetFrequencyForHla"/>.
/// </para>
/// </summary>
[TestFixture]
internal class ImputationEquivalenceTests
{
    private const int FrequencySetId = 7;
    private const string HfSetNomenclatureVersion = "3480";

    /// <summary>Every locus match prediction considers - i.e. <c>LocusSettings.MatchPredictionLoci</c>.</summary>
    private static readonly ISet<Locus> AllFiveLoci =
        new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 };

    private Fixture fixture;
    private ICompressedPhenotypeConverter converter;
    private IHaplotypeFrequencyService haplotypeFrequencyService;

    /// <summary>Frequencies the stubbed service will answer with, keyed exactly as the production code asks.</summary>
    private Dictionary<LociInfo<string>, decimal> frequencies;

    /// <summary>Every <c>excludedLoci</c> set the code under test asked a frequency for.</summary>
    private List<ISet<Locus>> excludedLociAsked;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        converter = Substitute.For<ICompressedPhenotypeConverter>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();

        frequencies = new Dictionary<LociInfo<string>, decimal>();
        excludedLociAsked = [];

        haplotypeFrequencyService.GetFrequencyForHla(default, default, default).ReturnsForAnyArgs(call =>
        {
            excludedLociAsked.Add(call.ArgAt<ISet<Locus>>(2));
            return Task.FromResult(frequencies.GetValueOrDefault(call.ArgAt<LociInfo<string>>(1)));
        });
    }

    // ---- The direct-lookup path: no excluded loci, so a survivor is one stored haplotype -----------------------

    [Test]
    public async Task Impute_OnTheFiveLocusKey_ReturnsEachGenotypeWithTheProductOfItsTwoHaplotypeFrequencies()
    {
        ArrangeFiveLocusSet();

        var result = await BuildService().Impute(AmbiguousAtAAndB(AllFiveLoci));

        // Three pairs from two survivors - (h1,h1), (h1,h2), (h2,h2) - because the pairing loop includes self-pairs.
        // Likelihood is f(pos1 haplotype) x f(pos2 haplotype) x 2 when any allowed locus is heterozygous, else x 1.
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 0.16m,
            [Genotype(a: ("a1", "a2"), b: ("b1", "b2"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 0.08m,
            [Genotype(a: ("a2", "a2"), b: ("b2", "b2"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 0.01m
        });

        result.Genotypes.Should().HaveCount(3);
        result.SumOfLikelihoods.Should().Be(0.25m);
    }

    [Test]
    public async Task Impute_OnTheFiveLocusKey_ExcludesNoLoci()
    {
        ArrangeFiveLocusSet();

        await BuildService().Impute(AmbiguousAtAAndB(AllFiveLoci));

        // excludedLoci = MatchPredictionLoci \ allowedLoci. Empty here, which is what makes this the one key whose
        // frequency lookups take the direct SetFrequencies path (ATL-233 §5b: FreqDirectHits > 0, FreqConsolidated 0).
        excludedLociAsked.Should().NotBeEmpty();
        excludedLociAsked.Should().AllSatisfy(excluded => excluded.Should().BeEmpty());
    }

    [Test]
    public async Task Impute_ResolvesOneFrequencyPerSurvivor_NotTwoPerGenotype()
    {
        ArrangeFiveLocusSet();

        await BuildService().Impute(AmbiguousAtAAndB(AllFiveLoci));

        // Two survivors produce three genotypes. The shipped code asked twice per genotype - six lookups, each an
        // await re-entering LazyCache's GetOrAddAsync. A frequency depends only on (set, haplotype, excluded loci), so
        // two answers cover every genotype. At corpus scale that ratio is 2 x 68,440 genotypes against 465.7
        // survivors for a tail donor, and it is the whole of ATL-233 T2's prize.
        await haplotypeFrequencyService.Received(2).GetFrequencyForHla(
            FrequencySetId, Arg.Any<LociInfo<string>>(), Arg.Any<ISet<Locus>>());
    }

    // ---- The consolidated path: the case T2's original mechanism would have broken -----------------------------

    [Test]
    public async Task Impute_OnAReducedKey_UsesTheConsolidatedFrequencyForTheCollapsedSurvivor()
    {
        // Two stored haplotypes agree at A, B and DRB1 and differ only at C. With C excluded they collapse to ONE
        // survivor, whose correct frequency is their SUM (0.4 + 0.2), not either individual value.
        ArrangeReducedKeySet();

        var result = await BuildService().Impute(AmbiguousAtAAndB(new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 }));

        // 0.6 x 0.6 x 1, 0.6 x 0.1 x 2, 0.1 x 0.1 x 1. An implementation carrying an individual haplotype frequency
        // would produce 0.16/0.08/0.01 (from 0.4) or 0.04/0.04/0.01 (from 0.2) - never these.
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"), drb1: ("r1", "r1"))] = 0.36m,
            [Genotype(a: ("a1", "a2"), b: ("b1", "b2"), drb1: ("r1", "r1"))] = 0.12m,
            [Genotype(a: ("a2", "a2"), b: ("b2", "b2"), drb1: ("r1", "r1"))] = 0.01m
        });

        result.SumOfLikelihoods.Should().Be(0.49m);
    }

    [Test]
    public async Task Impute_OnAReducedKey_AsksForTheFrequencyWithExactlyTheExcludedLoci()
    {
        ArrangeReducedKeySet();

        await BuildService().Impute(AmbiguousAtAAndB(new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 }));

        // The excluded set is what selects the consolidated dictionary, so getting it wrong silently returns a
        // frequency for a different grouping. Dpb1 is not a match-prediction locus and must not appear.
        excludedLociAsked.Should().NotBeEmpty();
        excludedLociAsked.Should().AllSatisfy(excluded =>
            excluded.Should().BeEquivalentTo(new[] { Locus.C, Locus.Dqb1 }));
    }

    // ---- The paths that need no frequency at all ----------------------------------------------------------------

    [Test]
    public async Task Impute_WhenUnambiguousAtEveryAllowedLocus_ReturnsOneGenotypeOfLikelihoodOneAndTouchesNoFrequency()
    {
        ArrangeFiveLocusSet();
        converter.StubGroups(SmallGGroupOnly(
            a: ["a1"], b: ["b1"], c: ["c1"], dqb1: ["q1"], drb1: ["r1"]));

        var result = await BuildService().Impute(AmbiguousAtAAndB(AllFiveLoci));

        // A single genotype is already certain, so the shipped code short-circuits to a likelihood of 1 without
        // consulting the frequency set at all. 59.15% of real donors take this branch.
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 1m
        });

        await haplotypeFrequencyService.DidNotReceiveWithAnyArgs().GetFrequencyForHla(default, default, default);
        await haplotypeFrequencyService.DidNotReceiveWithAnyArgs().GetAllHaplotypeFrequencies(default);
    }

    [Test]
    public async Task Impute_WhenNoHaplotypeExplainsTheTyping_ReturnsEmpty()
    {
        ArrangeFiveLocusSet();
        converter.StubGroups(SmallGGroupOnly(
            a: ["unrepresented-1", "unrepresented-2"], b: ["b1", "b2"], c: ["c1"], dqb1: ["q1"], drb1: ["r1"]));

        var result = await BuildService().Impute(AmbiguousAtAAndB(AllFiveLoci));

        result.Genotypes.Should().BeEmpty();
        result.GenotypeLikelihoods.Should().BeEmpty();
        result.SumOfLikelihoods.Should().Be(0);
    }

    // ---- Truncation, so a change to pool ordering cannot pass unnoticed ----------------------------------------

    [Test]
    public async Task Impute_WhenGenotypesExceedTheCap_KeepsTheMostLikely()
    {
        ArrangeFiveLocusSet();

        var result = await BuildService(maximumExpandedGenotypesPerInput: 2).Impute(AmbiguousAtAAndB(AllFiveLoci));

        // The two dearest of the three, and nothing about the third. This runs the equivalence assertion THROUGH
        // ExpandedGenotypeTruncater's OrderByDescending, which has no secondary sort key - so if a change to the
        // projected pool's enumeration order altered which genotypes survive, it would surface here.
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 0.16m,
            [Genotype(a: ("a1", "a2"), b: ("b1", "b2"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"))] = 0.08m
        });

        result.Genotypes.Should().HaveCount(2);
        result.SumOfLikelihoods.Should().Be(0.24m);
    }

    // ---- A set holding more than one typing category ------------------------------------------------------------

    [Test]
    public async Task Impute_WhenTheSetHoldsTwoTypingCategories_ExpandsAgainstBoth()
    {
        // ATL-233 T5's SQL found all 216 DEV sets are single-category SmallGGroup, so nothing exercises the
        // multi-category branch of the pool projection. T1 caches that projection, so it must be pinned.
        var interner = new HaplotypeInterner();
        var stored = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>
        {
            [interner.Intern("a1", "b1", c: null, dqb1: null, drb1: null)] =
                new(0.4m, HaplotypeTypingCategory.SmallGGroup),
            [interner.Intern("gA", "gB", c: null, dqb1: null, drb1: null)] =
                new(0.1m, HaplotypeTypingCategory.GGroup)
        };
        ArrangeSet(interner, stored);
        frequencies[Haplotype("a1", "b1")] = 0.4m;
        frequencies[Haplotype("gA", "gB")] = 0.1m;

        converter.StubGroups(new DataByResolution<PhenotypeInfo<ISet<string>>>
        {
            PGroup = new PhenotypeInfo<ISet<string>>(),
            GGroup = Groups(a: ["gA"], b: ["gB"]),
            // Ambiguous, because IsUnambiguousAtAllowedLoci reads SmallGGroup ONLY - a single group here would take
            // the short circuit and never touch the pool, whatever the GGroup side says.
            SmallGGroup = Groups(a: ["a1", "a2"], b: ["b1", "b2"])
        });

        var result = await BuildService().Impute(AmbiguousAtAAndB(new HashSet<Locus> { Locus.A, Locus.B }));

        // One survivor per category, so three pairs. Note the POSITION ORDER of the cross-category genotype: the pool
        // is merged GGroup, then PGroup, then SmallGGroup, and the pairing loop preserves that order - so the GGroup
        // haplotype lands at position 1. Cheap to get wrong, and a silent clinical change if the merge order moves.
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("gA", "gA"), b: ("gB", "gB"))] = 0.01m,
            [Genotype(a: ("gA", "a1"), b: ("gB", "b1"))] = 0.08m,
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"))] = 0.16m
        });

        result.SumOfLikelihoods.Should().Be(0.25m);
    }

    [Test]
    public async Task Impute_WhenSurvivorsOfDifferentCategoriesShareHlaNames_CollapsesThemToOneLikelihood()
    {
        // The one way a collapse is reachable. ToHlaNames() drops the typing category, so two genotypes can share a
        // string form - but SetFrequencies is keyed by HaplotypeKey ALONE, so a given haplotype has exactly one
        // category and two survivors can only share names if they differ at an EXCLUDED locus. Hence: two haplotypes
        // identical but for C, held at different categories, on a key that excludes C.
        var interner = new HaplotypeInterner();
        var stored = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>
        {
            [interner.Intern("a1", "b1", "c1", dqb1: null, drb1: "r1")] =
                new(0.4m, HaplotypeTypingCategory.SmallGGroup),
            [interner.Intern("a1", "b1", "c2", dqb1: null, drb1: "r1")] =
                new(0.2m, HaplotypeTypingCategory.GGroup),
            [interner.Intern("a2", "b2", "c1", dqb1: null, drb1: "r1")] =
                new(0.1m, HaplotypeTypingCategory.SmallGGroup)
        };
        ArrangeSet(interner, stored);

        // Both surviving forms of (a1,b1,·,·,r1) resolve to the SAME consolidated frequency, because the frequency is
        // a function of the names and the excluded loci - not of the typing category. That is what makes the collapse
        // harmless, and it is the assumption a pair-time likelihood must not break.
        frequencies[Haplotype("a1", "b1", c: null, dqb1: null, drb1: "r1")] = 0.6m;
        frequencies[Haplotype("a2", "b2", c: null, dqb1: null, drb1: "r1")] = 0.1m;

        converter.StubGroups(new DataByResolution<PhenotypeInfo<ISet<string>>>
        {
            PGroup = new PhenotypeInfo<ISet<string>>(),
            GGroup = Groups(a: ["a1"], b: ["b1"], drb1: ["r1"]),
            SmallGGroup = Groups(a: ["a1", "a2"], b: ["b1", "b2"], drb1: ["r1"])
        });

        var result = await BuildService().Impute(AmbiguousAtAAndB(new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 }));

        // Three survivors give SIX pairs, but only THREE distinct string genotypes: the GGroup and SmallGGroup forms
        // of (a1,b1,·,·,r1) are interchangeable by name. Every colliding pair carries an equal likelihood.
        result.Genotypes.Should().HaveCount(6);
        result.GenotypeLikelihoods.Should().BeEquivalentTo(new Dictionary<PhenotypeInfo<string>, decimal>
        {
            [Genotype(a: ("a1", "a1"), b: ("b1", "b1"), drb1: ("r1", "r1"))] = 0.36m,
            [Genotype(a: ("a1", "a2"), b: ("b1", "b2"), drb1: ("r1", "r1"))] = 0.12m,
            [Genotype(a: ("a2", "a2"), b: ("b2", "b2"), drb1: ("r1", "r1"))] = 0.01m
        });

        result.SumOfLikelihoods.Should().Be(0.49m);
    }

    // ---- T1: the projection is per set, not per donor -----------------------------------------------------------

    [Test]
    public async Task Impute_ForTwoDonorsOnTheSameSet_ProjectsThePoolOnce()
    {
        ArrangeFiveLocusSet();
        var entry = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId);
        var poolBeforeAnyDonor = entry.ProjectedPool;
        var service = BuildService();

        await service.Impute(AmbiguousAtAAndB(AllFiveLoci));
        await service.Impute(AmbiguousAtAAndB(AllFiveLoci));

        // T1's whole claim: neither donor re-projected. Reference identity is the assertion, because equality would
        // pass just as happily against a pool rebuilt per donor - which is exactly what shipped before.
        entry.ProjectedPool.Should().BeSameAs(poolBeforeAnyDonor);
        entry.ProjectedPool.SmallGGroup.Should().BeSameAs(poolBeforeAnyDonor.SmallGGroup);
    }

    // ---- Arrangement --------------------------------------------------------------------------------------------

    private IGenotypeImputationService BuildService(int maximumExpandedGenotypesPerInput = 2000) =>
        new GenotypeImputationService(
            new CompressedPhenotypeExpander(converter, haplotypeFrequencyService),
            Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            new GenotypeImputationSettings { MaximumExpandedGenotypesPerInput = maximumExpandedGenotypesPerInput });

    /// <summary>
    /// Two haplotypes differing at A and B, fully typed at C/DQB1/DRB1 - so with all five loci allowed, no locus is
    /// excluded and each survivor corresponds to exactly one stored haplotype.
    /// </summary>
    private void ArrangeFiveLocusSet()
    {
        var interner = new HaplotypeInterner();
        var stored = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>
        {
            [interner.Intern("a1", "b1", "c1", "q1", "r1")] = new(0.4m, HaplotypeTypingCategory.SmallGGroup),
            [interner.Intern("a2", "b2", "c1", "q1", "r1")] = new(0.1m, HaplotypeTypingCategory.SmallGGroup)
        };
        ArrangeSet(interner, stored);

        frequencies[Haplotype("a1", "b1", "c1", "q1", "r1")] = 0.4m;
        frequencies[Haplotype("a2", "b2", "c1", "q1", "r1")] = 0.1m;

        converter.StubGroups(SmallGGroupOnly(
            a: ["a1", "a2"], b: ["b1", "b2"], c: ["c1"], dqb1: ["q1"], drb1: ["r1"]));
    }

    /// <summary>
    /// Three haplotypes, two of which differ ONLY at C. On a key that excludes C they collapse to one survivor, and
    /// the frequency the code must use is the sum of the two.
    /// </summary>
    private void ArrangeReducedKeySet()
    {
        var interner = new HaplotypeInterner();
        var stored = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>
        {
            [interner.Intern("a1", "b1", "c1", "q1", "r1")] = new(0.4m, HaplotypeTypingCategory.SmallGGroup),
            [interner.Intern("a1", "b1", "c2", "q1", "r1")] = new(0.2m, HaplotypeTypingCategory.SmallGGroup),
            [interner.Intern("a2", "b2", "c1", "q1", "r1")] = new(0.1m, HaplotypeTypingCategory.SmallGGroup)
        };
        ArrangeSet(interner, stored);

        // Keyed by the survivor as the code presents it: nulled at the excluded loci (C, DQB1) and at Dpb1. The 0.6
        // is the consolidated sum the real HaplotypeFrequencyCache would answer with.
        frequencies[Haplotype("a1", "b1", c: null, dqb1: null, drb1: "r1")] = 0.6m;
        frequencies[Haplotype("a2", "b2", c: null, dqb1: null, drb1: "r1")] = 0.1m;

        // C and DQB1 left null, so the pool filter treats them as a wildcard - the untyped-locus case.
        converter.StubGroups(SmallGGroupOnly(
            a: ["a1", "a2"], b: ["b1", "b2"], drb1: ["r1"]));
    }

    private void ArrangeSet(HaplotypeInterner interner, Dictionary<HaplotypeKey, HaplotypeFrequencyValue> stored)
    {
        // Interned exactly as HaplotypeFrequencyCache.BuildEntryFromDatabase does, so the expander's ReverseLookup
        // round trip - the thing T1 caches - is the real one.
        var entry = new FrequencySetCacheEntry
        {
            SetFrequencies = stored.ToFrozenDictionary(),
            Interner = interner
        };

        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(entry);
    }

    private ImputationInput AmbiguousAtAAndB(ISet<Locus> allowedLoci) => new()
    {
        SubjectData = new SubjectData(
            new PhenotypeInfo<string>(),
            new SubjectFrequencySet(
                new HfSet { Id = FrequencySetId, HlaNomenclatureVersion = HfSetNomenclatureVersion },
                fixture.Create<string>())),
        MatchPredictionParameters = new MatchPredictionParameters(allowedLoci)
    };

    // ---- Builders -----------------------------------------------------------------------------------------------

    private static LociInfo<string> Haplotype(
        string a = null, string b = null, string c = null, string dqb1 = null, string drb1 = null) =>
        new(valueA: a, valueB: b, valueC: c, valueDqb1: dqb1, valueDrb1: drb1);

    private static PhenotypeInfo<string> Genotype(
        (string Position1, string Position2) a = default,
        (string Position1, string Position2) b = default,
        (string Position1, string Position2) c = default,
        (string Position1, string Position2) dqb1 = default,
        (string Position1, string Position2) drb1 = default) =>
        new PhenotypeInfo<string>()
            .SetLocus(Locus.A, a.Position1, a.Position2)
            .SetLocus(Locus.B, b.Position1, b.Position2)
            .SetLocus(Locus.C, c.Position1, c.Position2)
            .SetLocus(Locus.Dqb1, dqb1.Position1, dqb1.Position2)
            .SetLocus(Locus.Drb1, drb1.Position1, drb1.Position2);

    private static DataByResolution<PhenotypeInfo<ISet<string>>> SmallGGroupOnly(
        string[] a = null, string[] b = null, string[] c = null, string[] dqb1 = null, string[] drb1 = null) =>
        new()
        {
            // Null at GGroup/PGroup, matching a set that holds no haplotypes at those resolutions - the real shape of
            // all 216 DEV sets per ATL-233 T5.
            GGroup = new PhenotypeInfo<ISet<string>>(),
            PGroup = new PhenotypeInfo<ISet<string>>(),
            SmallGGroup = Groups(a, b, c, dqb1, drb1)
        };

    /// <summary>The same groups at both positions. A null locus is left null, i.e. untyped, i.e. a wildcard.</summary>
    private static PhenotypeInfo<ISet<string>> Groups(
        string[] a = null, string[] b = null, string[] c = null, string[] dqb1 = null, string[] drb1 = null)
    {
        var groups = new PhenotypeInfo<ISet<string>>();

        foreach (var (locus, names) in new[]
                 {
                     (Locus.A, a), (Locus.B, b), (Locus.C, c), (Locus.Dqb1, dqb1), (Locus.Drb1, drb1)
                 })
        {
            if (names != null)
            {
                groups = groups.SetLocus(locus, (ISet<string>)new HashSet<string>(names));
            }
        }

        return groups;
    }
}
