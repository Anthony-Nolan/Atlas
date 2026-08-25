using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.CompressedPhenotypeExpansion;

/// <summary>
/// ATL-233 T5: the phenotype is converted to the typing categories that get read, and to no others. The conversion was
/// 39.5% of imputation's measured cost (A1h) and two thirds of it was dead work, in two distinct ways - the unambiguous
/// short circuit reads SmallGGroup alone, and a category the frequency set holds no haplotypes in can change nothing.
///
/// <para>
/// The second of those is a change in <b>which lookups happen</b>, so it is the one that has to be pinned by test rather
/// than by argument: every set in DEV holds SmallGGroup only, and these assertions are what stops that becoming an
/// assumption baked into the code. A set holding two categories must still convert both.
/// </para>
/// </summary>
[TestFixture]
[NonParallelizable]
internal class TypingCategoryConversionTests
{
    private const int FrequencySetId = 41;

    private Fixture fixture;
    private ICompressedPhenotypeConverter converter;
    private IHaplotypeFrequencyService haplotypeFrequencyService;
    private CompressedPhenotypeExpander sut;

    private string smallGA1;
    private string smallGA2;
    private string gGroupA;
    private string sharedB;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        converter = Substitute.For<ICompressedPhenotypeConverter>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();

        smallGA1 = fixture.Create<string>();
        smallGA2 = fixture.Create<string>();
        gGroupA = fixture.Create<string>();
        sharedB = fixture.Create<string>();

        sut = new CompressedPhenotypeExpander(converter, haplotypeFrequencyService);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenUnambiguousAtAllowedLoci_ConvertsSmallGGroupOnly()
    {
        // One group at each allowed position, so the expansion returns before any pool is involved. Nothing on that
        // branch reads GGroup or PGroup, whatever the set holds - which is why this needs no set at all.
        converter.StubGroups(Groups(smallGAtA: [smallGA1]));

        await Expand();

        await converter.Received(1).ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.SmallGGroup);
        await converter.DidNotReceive().ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.GGroup);
        await converter.DidNotReceive().ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.PGroup);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSetHoldsSmallGGroupOnly_DoesNotConvertTheOtherTwoCategories()
    {
        // The shape of every one of the 216 frequency sets we hold, on the expanding path this time:
        // both other pool arrays are empty, so both other conversions cannot affect the result.
        converter.StubGroups(Groups(smallGAtA: [smallGA1, smallGA2]));
        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(Pool(
            (smallGA1, HaplotypeTypingCategory.SmallGGroup),
            (smallGA2, HaplotypeTypingCategory.SmallGGroup)));

        await Expand();

        // Fetching the pool at all is what says the unambiguous short circuit was not taken: that branch returns before
        // any set is involved, so the two categories below are skipped for a different reason than the one under test.
        await haplotypeFrequencyService.Received(1).GetAllHaplotypeFrequencies(FrequencySetId);

        await converter.Received(1).ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.SmallGGroup);
        await converter.DidNotReceive().ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.GGroup);
        await converter.DidNotReceive().ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.PGroup);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSetHoldsTwoCategories_ConvertsBoth()
    {
        converter.StubGroups(Groups(smallGAtA: [smallGA1, smallGA2], gGroupAtA: [gGroupA]));
        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(Pool(
            (smallGA1, HaplotypeTypingCategory.SmallGGroup),
            (gGroupA, HaplotypeTypingCategory.GGroup)));

        await Expand();

        await converter.Received(1).ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.SmallGGroup);
        await converter.Received(1).ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.GGroup);

        // Still not PGroup: the rule is what the set holds, not "more than one category means convert everything".
        await converter.DidNotReceive().ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), HaplotypeTypingCategory.PGroup);
    }

    [Test]
    public async Task ExpandCompressedPhenotype_WhenTheSetHoldsTwoCategories_KeepsTheSurvivorsOfBoth()
    {
        // The assertion that matters: this counts the haplotypes of BOTH categories through the filter, so a rule that
        // converted too few categories cannot pass it. (Today such a bug would throw when the filter read the missing
        // groups, which is the safe failure - but the count is what pins the behaviour, not the exception.)
        converter.StubGroups(Groups(smallGAtA: [smallGA1, smallGA2], gGroupAtA: [gGroupA]));
        haplotypeFrequencyService.GetAllHaplotypeFrequencies(FrequencySetId).Returns(Pool(
            (smallGA1, HaplotypeTypingCategory.SmallGGroup),
            (smallGA2, HaplotypeTypingCategory.SmallGGroup),
            (gGroupA, HaplotypeTypingCategory.GGroup),
            (fixture.Create<string>(), HaplotypeTypingCategory.GGroup)));

        var expanded = await Expand();

        // Two of the SmallGGroup haplotypes and one of the two GGroup ones - the other names an A group the subject does
        // not have, so the filter drops it. S = 3 of H = 4.
        expanded.Haplotypes.Count.Should().Be(3);
    }

    /// <summary>
    /// Runs the real expander. Which categories were converted is asserted on the converter substitute; the surviving
    /// pool is read off the result.
    /// </summary>
    private async Task<ExpandedGenotypes> Expand()
    {
        return await sut.ExpandCompressedPhenotype(new CompressedPhenotypeExpanderInput
        {
            Phenotype = new PhenotypeInfo<string>(),
            HfSetId = FrequencySetId,
            HfSetHlaNomenclatureVersion = fixture.Create<string>(),
            MatchPredictionParameters = new MatchPredictionParameters(new HashSet<Locus> { Locus.A, Locus.B })
        });
    }

    /// <summary>
    /// The subject's groups at A, per category, with one shared group at B. PGroup is left null throughout: a category
    /// the caller does not ask for is never converted, so a null there is what production would hold - and if the
    /// expander ever did read it, the null would say so loudly.
    /// </summary>
    private DataByResolution<PhenotypeInfo<ISet<string>>> Groups(string[] smallGAtA, string[] gGroupAtA = null) =>
        new()
        {
            SmallGGroup = AtAAndB(smallGAtA),
            GGroup = gGroupAtA == null ? null : AtAAndB(gGroupAtA)
        };

    private PhenotypeInfo<ISet<string>> AtAAndB(string[] atA) =>
        new PhenotypeInfo<ISet<string>>()
            .SetLocus(Locus.A, new HashSet<string>(atA))
            .SetLocus(Locus.B, new HashSet<string> { sharedB });

    /// <summary>
    /// A set of haplotypes, each with its own typing category, interned exactly as
    /// <c>HaplotypeFrequencyCache.BuildEntryFromDatabase</c> interns them - so the categories reaching
    /// <c>FrequencySetCacheEntry.ProjectedPool</c> are the real thing rather than a hand-built projection.
    /// </summary>
    private FrequencySetCacheEntry Pool(params (string AtA, HaplotypeTypingCategory Category)[] haplotypes)
    {
        var interner = new HaplotypeInterner();
        var frequencies = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();

        foreach (var (atA, category) in haplotypes)
        {
            frequencies.Add(
                interner.Intern(atA, sharedB, c: null, dqb1: null, drb1: null),
                new HaplotypeFrequencyValue(fixture.Create<decimal>(), category));
        }

        return new FrequencySetCacheEntry
        {
            SetFrequencies = frequencies.ToFrozenDictionary(),
            Interner = interner
        };
    }
}
