using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HlaConversion;
using Atlas.MatchPrediction.Services.MatchProbability;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

/// <summary>
/// Characterisation tests for <see cref="GenotypeConverter"/>, which had no unit test at all before this fixture.
/// <see cref="IGenotypeConverter.ConvertGenotypesForMatchCalculation"/> converts one distinct (locus, name, typing
/// category) triple at a time, rather than one genotype position at a time.
///
/// <para>
/// They are an oracle rather than a snapshot: every expected P group is stated here, so the fixture passes against the
/// per-position implementation as well and only fails if the rewrite changed an answer. The two that matter most are
/// <see cref="ConvertGenotypesForMatchCalculation_ConvertsEachDistinctTripleExactlyOnce"/>, which pins the lever, and
/// <see cref="ConvertGenotypesForMatchCalculation_WhenOneNameAppearsUnderTwoTypingCategories_ConvertsItUnderBoth"/>,
/// which pins the case that can only go wrong: a memo keyed on the name alone would answer one of those two triples
/// with the other's P group.
/// </para>
/// </summary>
[TestFixture]
internal class GenotypeConverterTests
{
    private const string HfSetNomenclatureVersion = "3480";

    /// <summary>Every locus match prediction considers - i.e. <c>LocusSettings.MatchPredictionLoci</c>.</summary>
    private static readonly ISet<Locus> AllFiveLoci =
        new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 };

    private Fixture fixture;

    private IHlaCategorisationService categoriser;
    private IHlaToTargetCategoryConverter hlaToTargetCategoryConverter;
    private IGGroupToPGroupConverter gGroupConverter;
    private ISmallGGroupToPGroupConverter smallGGroupConverter;

    /// <summary>
    /// P group by (locus, name), one map per converter, because the same name at the same locus is a different triple
    /// under a different typing category and the two converters may legitimately disagree. A name absent from the map
    /// converts to null - i.e. it has no P group.
    /// </summary>
    private Dictionary<(Locus, string), string> smallGPGroups;

    private Dictionary<(Locus, string), string> gPGroups;

    /// <summary>Every (locus, name) each converter was asked to convert, in order, including repeats.</summary>
    private List<(Locus Locus, string Hla)> smallGRequests;
    private List<(Locus Locus, string Hla)> gRequests;

    private GenotypeConverter sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        categoriser = Substitute.For<IHlaCategorisationService>();
        hlaToTargetCategoryConverter = Substitute.For<IHlaToTargetCategoryConverter>();
        gGroupConverter = Substitute.For<IGGroupToPGroupConverter>();
        smallGGroupConverter = Substitute.For<ISmallGGroupToPGroupConverter>();

        var hlaMetadataDictionaryFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
        hlaMetadataDictionaryFactory.BuildDictionary(default).ReturnsForAnyArgs(Substitute.For<IHlaMetadataDictionary>());

        smallGPGroups = new Dictionary<(Locus, string), string>();
        gPGroups = new Dictionary<(Locus, string), string>();
        smallGRequests = [];
        gRequests = [];

        RecordAndConvert(smallGGroupConverter, smallGRequests, smallGPGroups);
        RecordAndConvert(gGroupConverter, gRequests, gPGroups);

        sut = new GenotypeConverter(
            hlaMetadataDictionaryFactory,
            categoriser,
            hlaToTargetCategoryConverter,
            gGroupConverter,
            smallGGroupConverter,
            Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>());
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_ReturnsThePGroupOfEveryPosition()
    {
        ArrangePGroups(("a1", "a1P"), ("a2", "a2P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        var genotype = Genotype(a: ("a1", "a2"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.Should().Be(Names(
            a: ("a1P", "a2P"), b: ("b1P", "b1P"), c: ("c1P", "c1P"), dqb1: ("q1P", "q1P"), drb1: ("r1P", "r1P")));
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_ConvertsEachDistinctTripleExactlyOnce()
    {
        ArrangePGroups(("a1", "a1P"), ("a2", "a2P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        // Three genotypes over two haplotypes at A, which is the shape the pairing loop produces - (h1,h1), (h1,h2),
        // (h2,h2). The per-position implementation asked for 30 conversions here; the distinct triples number six.
        var converted = await Convert(
            Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1")),
            Genotype(a: ("a1", "a2"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1")),
            Genotype(a: ("a2", "a2"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1")));

        converted.Should().HaveCount(3);
        smallGRequests.Should().BeEquivalentTo(new[]
        {
            (Locus.A, "a1"), (Locus.A, "a2"), (Locus.B, "b1"), (Locus.C, "c1"), (Locus.Dqb1, "q1"), (Locus.Drb1, "r1")
        });
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_DoesNotShareOneLocusPGroupWithAnother()
    {
        // The same name at two loci is two triples, because a P group is only defined within a locus. A memo keyed on
        // the name alone would hand B's conversion back for A's position.
        ArrangePGroups((Locus.A, "shared", "aP"), (Locus.B, "shared", "bP"));
        ArrangePGroups(("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        var genotype = Genotype(
            a: ("shared", "shared"), b: ("shared", "shared"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.A.Should().Be(new LocusInfo<string>("aP", "aP"));
        converted.Single().StringMatchableResolution.B.Should().Be(new LocusInfo<string>("bP", "bP"));
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_WhenOneNameAppearsUnderTwoTypingCategories_ConvertsItUnderBoth()
    {
        // The case that can only go wrong. A frequency set holds its typing category per row, so one genotype can pair
        // a small-g-typed haplotype with a G-group-typed one, and the two use
        // different converters. Keying the memo on the name alone would answer one position with the other's P group.
        ArrangePGroups(("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));
        smallGPGroups[(Locus.A, "ambiguous")] = "smallGP";
        gPGroups[(Locus.A, "ambiguous")] = "gGroupP";

        var genotype = new GenotypeOfKnownTypingCategory(
            valueA: new LocusInfo<HlaAtKnownTypingCategory>(
                new HlaAtKnownTypingCategory("ambiguous", HaplotypeTypingCategory.SmallGGroup),
                new HlaAtKnownTypingCategory("ambiguous", HaplotypeTypingCategory.GGroup)),
            valueB: SmallG("b1", "b1"),
            valueC: SmallG("c1", "c1"),
            valueDqb1: SmallG("q1", "q1"),
            valueDrb1: SmallG("r1", "r1"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.A.Should().Be(new LocusInfo<string>("smallGP", "gGroupP"));
        smallGRequests.Should().Contain((Locus.A, "ambiguous"));
        gRequests.Should().BeEquivalentTo(new[] { (Locus.A, "ambiguous") });
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_WhenOnePositionHasNoPGroup_TakesThePairedPositionsPGroup()
    {
        // "a2" has no P group - a null-expressing allele expresses no protein - so A position 2 takes position 1's,
        // which is README_MatchPredictionAlgorithm.md's null-allele rule.
        ArrangePGroups(("a1", "a1P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        var genotype = Genotype(a: ("a1", "a2"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.A.Should().Be(new LocusInfo<string>("a1P", "a1P"));
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_WhenNeitherPositionHasAPGroup_LeavesTheLocusAbsent()
    {
        ArrangePGroups(("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        var genotype = Genotype(a: ("a1", "a2"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await Convert(genotype);

        // Absent at both, not filled in from the other position - the locus is then untyped to the match calculator.
        converted.Single().StringMatchableResolution.A.Should().Be(new LocusInfo<string>(null, null));
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_LeavesAnUntypedLocusAbsentWithoutConvertingIt()
    {
        ArrangePGroups(("a1", "a1P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        // DPB1 is not a match prediction locus, so a genotype never carries one.
        var genotype = Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.Dpb1.Should().Be(new LocusInfo<string>(null, null));
        smallGRequests.Should().NotContain(request => request.Locus == Locus.Dpb1);
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_WhenTypedToPGroup_PassesTheNameThroughWithoutConverting()
    {
        var genotype = new GenotypeOfKnownTypingCategory(
            valueA: PGroupTyped("a1P", "a2P"),
            valueB: PGroupTyped("b1P", "b1P"),
            valueC: PGroupTyped("c1P", "c1P"),
            valueDqb1: PGroupTyped("q1P", "q1P"),
            valueDrb1: PGroupTyped("r1P", "r1P"));

        var converted = await Convert(genotype);

        converted.Single().StringMatchableResolution.Should().Be(Names(
            a: ("a1P", "a2P"), b: ("b1P", "b1P"), c: ("c1P", "c1P"), dqb1: ("q1P", "q1P"), drb1: ("r1P", "r1P")));
        smallGRequests.Should().BeEmpty();
        gRequests.Should().BeEmpty();
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_CarriesTheGenotypesOwnNamesAndItsLikelihood()
    {
        ArrangePGroups(("a1", "a1P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));

        var genotype = Genotype(a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));
        var likelihood = fixture.Create<decimal>();

        var converted = await sut.ConvertGenotypesForMatchCalculation(
            Input([genotype], new Dictionary<HfSetGenotypeNames, decimal> { [genotype.ToHlaNames()] = likelihood }));

        // HaplotypeResolution is the stored-resolution name form, NOT the P groups above - the two are what
        // GenotypeAtDesiredResolutions exists to hold side by side.
        converted.Single().HaplotypeResolution.Should().Be(Names(
            a: ("a1", "a1"), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1")));
        converted.Single().GenotypeLikelihood.Should().Be(likelihood);
    }

    [Test]
    public async Task ConvertGenotypesForMatchCalculation_WhenTheSubmittedTypingHasANullAllele_ConvertsThatLocusAsHomozygous()
    {
        const string nullAllele = "01:01:01:02N";
        const string nullAlleleSmallG = "nullAlleleSmallG";

        ArrangePGroups(("a1", "a1P"), ("b1", "b1P"), ("c1", "c1P"), ("q1", "q1P"), ("r1", "r1P"));
        smallGPGroups[(Locus.A, nullAlleleSmallG)] = "shouldNotBeReached";

        categoriser.IsNullAllele(nullAllele).Returns(true);
        hlaToTargetCategoryConverter.ConvertHlaWithLoggingAndRetryOnFailure(default, default, default)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<string>>([nullAlleleSmallG]));

        // The subject was submitted with a null allele at A position 2, and the imputed genotype carries that null
        // allele's group at the same position. The locus must be converted as homozygous for the expressing allele.
        var genotype = Genotype(
            a: ("a1", nullAlleleSmallG), b: ("b1", "b1"), c: ("c1", "c1"), dqb1: ("q1", "q1"), drb1: ("r1", "r1"));

        var converted = await sut.ConvertGenotypesForMatchCalculation(new GenotypeConverterInput
        {
            CompressedPhenotype = new HfSetGenotypeNames(valueA: new LocusInfo<string>("01:01", nullAllele)),
            AllowedLoci = AllFiveLoci,
            Genotypes = [new ImputedGenotype(genotype, genotype.ToHlaNames(), fixture.Create<decimal>())],
            HfSetHlaNomenclatureVersion = HfSetNomenclatureVersion,
            SubjectLogDescription = fixture.Create<string>()
        });

        converted.Single().StringMatchableResolution.A.Should().Be(new LocusInfo<string>("a1P", "a1P"));
        // The substituted-away name is never converted: the distinct-triple pass sees the adjusted genotype, which is
        // the property that lets that pass and the build pass each apply the adjustment independently.
        smallGRequests.Should().NotContain((Locus.A, nullAlleleSmallG));
    }

    // ---- Arrangement helpers ---------------------------------------------------------------------------------------

    /// <summary>Records what the converter was asked, and answers from its own map.</summary>
    private static void RecordAndConvert(
        IHlaConverter converter,
        List<(Locus, string)> requests,
        Dictionary<(Locus, string), string> pGroups)
    {
        converter.ConvertHlaWithLoggingAndRetryOnFailure(default, default, default).ReturnsForAnyArgs(call =>
        {
            var locus = call.ArgAt<Locus>(1);
            var hla = call.ArgAt<string>(2);
            requests.Add((locus, hla));

            // A name with no P group answers with a single null, which is what the real converters do when the lookup
            // fails - ConvertHlaWithLoggingAndRetryOnFailure logs and returns rather than throwing.
            return Task.FromResult<IEnumerable<string>>([pGroups.GetValueOrDefault((locus, hla))]);
        });
    }

    /// <summary>Gives a name the same P group at every locus, which is enough for tests that are not about loci.</summary>
    private void ArrangePGroups(params (string Hla, string PGroup)[] entries)
    {
        foreach (var (hla, pGroup) in entries)
        {
            foreach (var locus in AllFiveLoci)
            {
                smallGPGroups[(locus, hla)] = pGroup;
            }
        }
    }

    private void ArrangePGroups(params (Locus Locus, string Hla, string PGroup)[] entries)
    {
        foreach (var (locus, hla, pGroup) in entries)
        {
            smallGPGroups[(locus, hla)] = pGroup;
        }
    }

    private async Task<IList<GenotypeAtDesiredResolutions>> Convert(params GenotypeOfKnownTypingCategory[] genotypes)
    {
        var likelihoods = genotypes.ToDictionary(genotype => genotype.ToHlaNames(), _ => fixture.Create<decimal>());

        return (await sut.ConvertGenotypesForMatchCalculation(Input(genotypes, likelihoods))).ToList();
    }

    /// <summary>
    /// The name form and the likelihood arrive attached to each genotype, so this helper does what the truncater does
    /// in production: the converter itself never derives a genotype's name form.
    /// </summary>
    private GenotypeConverterInput Input(
        IEnumerable<GenotypeOfKnownTypingCategory> genotypes,
        IReadOnlyDictionary<HfSetGenotypeNames, decimal> likelihoods) =>
        new()
        {
            // Untyped everywhere, so no position is a null allele and the null-allele adjustment does not apply. The
            // one test that needs it builds its own input.
            CompressedPhenotype = new HfSetGenotypeNames(),
            AllowedLoci = AllFiveLoci,
            Genotypes = genotypes
                .Select(genotype => new ImputedGenotype(genotype, genotype.ToHlaNames(), likelihoods[genotype.ToHlaNames()]))
                .ToList(),
            HfSetHlaNomenclatureVersion = HfSetNomenclatureVersion,
            SubjectLogDescription = fixture.Create<string>()
        };

    /// <summary>
    /// Named arguments throughout: <see cref="PhenotypeInfo{T}"/>'s positional constructor is
    /// (A, B, C, Dpb1, Dqb1, Drb1), and a genotype has no DPB1.
    /// </summary>
    private static GenotypeOfKnownTypingCategory Genotype(
        (string, string) a,
        (string, string) b,
        (string, string) c,
        (string, string) dqb1,
        (string, string) drb1) =>
        new(
            valueA: SmallG(a.Item1, a.Item2),
            valueB: SmallG(b.Item1, b.Item2),
            valueC: SmallG(c.Item1, c.Item2),
            valueDqb1: SmallG(dqb1.Item1, dqb1.Item2),
            valueDrb1: SmallG(drb1.Item1, drb1.Item2));

    private static HfSetGenotypeNames Names(
        (string, string) a,
        (string, string) b,
        (string, string) c,
        (string, string) dqb1,
        (string, string) drb1) =>
        new(
            valueA: new LocusInfo<string>(a.Item1, a.Item2),
            valueB: new LocusInfo<string>(b.Item1, b.Item2),
            valueC: new LocusInfo<string>(c.Item1, c.Item2),
            valueDqb1: new LocusInfo<string>(dqb1.Item1, dqb1.Item2),
            valueDrb1: new LocusInfo<string>(drb1.Item1, drb1.Item2));

    private static LocusInfo<HlaAtKnownTypingCategory> SmallG(string position1, string position2) =>
        new(
            new HlaAtKnownTypingCategory(position1, HaplotypeTypingCategory.SmallGGroup),
            new HlaAtKnownTypingCategory(position2, HaplotypeTypingCategory.SmallGGroup));

    private static LocusInfo<HlaAtKnownTypingCategory> PGroupTyped(string position1, string position2) =>
        new(
            new HlaAtKnownTypingCategory(position1, HaplotypeTypingCategory.PGroup),
            new HlaAtKnownTypingCategory(position2, HaplotypeTypingCategory.PGroup));
}
