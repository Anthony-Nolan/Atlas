using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

// Aliases, not wrapper types, and the difference is the point. This file is where three of the several meanings of
// PhenotypeInfo<string> meet, one method apart, and nothing but these names tells them apart:
//
//   SubmittedPhenotype        - the subject's typing as submitted: allele, MAC, XX code or serology. Any resolution,
//                               honestly so, which is why it gets an alias and never a wrapper type.
//   PossibleGroupsPerPosition - that typing expanded to the set of every group name each position COULD be, one
//                               typing category at a time. An ambiguous typing yields many per position.
//   HfSetGenotypeNames        - a genotype's names at the resolution the haplotype frequency set stores them, which
//                               is per row: P group, or G group where a null allele meant no P group existed. The
//                               typing category is ERASED, so two survivors differing only in category MUST
//                               collapse to one key.
//   HfSetHaplotypeNames       - the same, for ONE haplotype: one name per locus rather than two.
//
// An alias is file-scoped and erases to string, so it buys documentation at the declaration and no type safety at
// all. What it buys is that a reader of a declaration learns what the value is without leaving the file.
using SubmittedPhenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;
using PossibleGroupsPerPosition =
    Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<System.Collections.Generic.ISet<string>>;

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;

internal class CompressedPhenotypeExpanderInput
{
    /// <summary>
    /// Given phenotype. Can be of any supported HLA resolution.
    /// </summary>
    public SubmittedPhenotype Phenotype { get; set; }

    /// <summary>
    /// Haplotype Frequency Set Id - used to fetch haplotypes, if needed
    /// </summary>
    public int HfSetId { get; set; }

    /// <summary>
    /// HLA nomenclature version of Haplotype Frequency Set
    /// </summary>
    public string HfSetHlaNomenclatureVersion { get; set; }

    /// <inheritdoc cref="Common.Public.Models.MatchPrediction.MatchPredictionParameters" />
    public MatchPredictionParameters MatchPredictionParameters { get; set; }
}

internal interface ICompressedPhenotypeExpander
{
    /// <summary>
    /// Expands an ambiguous phenotype to GGroup resolution, then transforms into all possible permutations of the given hla representations.
    /// Does not consider phase - so the results cannot necessarily be considered Diplotypes.
    /// </summary>
    public Task<ExpandedGenotypes> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input);
}

/// <summary>
/// The genotypes an expansion produced, and the likelihood of each.
///
/// <para>
/// The likelihoods come back <b>with</b> the genotypes because this is the only place they are cheap. A genotype is a
/// pair of pooled haplotypes, so its likelihood is the product of their two frequencies - and a frequency is a pure
/// function of (set, haplotype, excluded loci). It therefore needs one resolution per <i>survivor</i>, which is only
/// possible while the pair is still in hand.
/// </para>
///
/// <para>
/// <b><see cref="Likelihoods"/> is keyed by <see cref="GenotypeNameKey"/> - the ids of the genotype's two haplotype
/// name forms - not by the name form itself.</b> The key still <i>means</i> "this genotype's HLA names", matching
/// <c>ImputedGenotypes.GenotypeLikelihoods</c>, so its count is the DISTINCT genotype count while
/// <see cref="GenotypePairs"/> keeps typing category and so may hold more; eight bytes carry that identity instead of
/// seven heap objects. See <see cref="GenotypeNameKey"/> for why the two are the same equality rather than an
/// approximation of it.
/// </para>
///
/// <para>
/// <see cref="GenotypeNameKeys"/> is <see cref="GenotypePairs"/> index for index, so
/// <c>ExpandedGenotypeTruncater</c> can test a genotype's membership of the kept key set without re-deriving anything,
/// and <see cref="MaterialiseNames"/> builds the name form for the survivors only.
/// </para>
///
/// <para>
/// <b><see cref="GenotypePairs"/> is a list, not a set, and cannot lose an entry to de-duplication.</b> The survivor
/// list it indexes comes out of a <c>HashSet</c>, so its members are distinct; <c>PhenotypeInfo</c> equality is
/// positional and <c>HlaAtKnownTypingCategory</c>'s includes the typing category. Distinct <c>(i, j)</c> therefore
/// always give a distinct genotype, and a set here could only hash every genotype to find nothing to merge.
/// </para>
///
/// <para>
/// <b>A genotype is carried as two pool indices plus an eight-byte name key, and <i>nothing</i> is built until
/// truncation has chosen.</b> A <c>PhenotypeInfo&lt;T&gt;</c> is seven objects - itself and one <c>LocusInfo</c> per
/// locus - and a capped donor keeps 2,000 genotypes out of up to 1.65M pairs. Neither the category form nor the name
/// form is read before <c>ExpandedGenotypeTruncater</c> has decided which keys survive. Both a
/// <see cref="GenotypePair"/> and a <see cref="GenotypeNameKey"/> are eight bytes in a contiguous list and allocate
/// nothing.
/// </para>
/// </summary>
internal readonly record struct ExpandedGenotypes(
    IReadOnlyList<LociInfo<HlaAtKnownTypingCategory>> Haplotypes,
    List<GenotypePair> GenotypePairs,
    List<GenotypeNameKey> GenotypeNameKeys,
    Dictionary<GenotypeNameKey, decimal> Likelihoods,
    IReadOnlyList<HfSetHaplotypeNames> HaplotypeNamesById)
{
    /// <summary>Pre-truncation genotype count - the number of pairs the expansion kept.</summary>
    public int GenotypeCount => GenotypePairs?.Count ?? 0;

    /// <summary>
    /// The genotype at <paramref name="index"/>, built now. Called for the genotypes truncation keeps, not for the
    /// ones it discards - which is the whole point of holding the pair rather than the phenotype.
    /// </summary>
    public PhenotypeInfo<HlaAtKnownTypingCategory> Materialise(int index)
    {
        var pair = GenotypePairs[index];

        return new PhenotypeInfo<HlaAtKnownTypingCategory>(Haplotypes[pair.Haplotype1], Haplotypes[pair.Haplotype2]);
    }

    /// <summary>
    /// The HLA-name form <paramref name="key"/> stands for, built now. Called once per genotype <b>truncation keeps</b>
    /// - at most the cap - rather than once per genotype the pairing loop examined.
    /// </summary>
    /// <remarks>
    /// Identical to <c>Materialise(index).ToHlaNames()</c> for any index whose key this is, and that identity is what
    /// lets <c>GenotypeConverter</c> keep looking its likelihood up by the genotype's own name form: both sides build
    /// <c>PhenotypeInfo(names of haplotype 1, names of haplotype 2)</c> from the same two <c>LociInfo</c>.
    /// </remarks>
    public HfSetGenotypeNames MaterialiseNames(GenotypeNameKey key) =>
        new(HaplotypeNamesById[key.Name1], HaplotypeNamesById[key.Name2]);
}

/// <summary>
/// A genotype as the two pool haplotypes it is a pair of, by index into <see cref="ExpandedGenotypes.Haplotypes"/>.
/// Position 1 is <see cref="Haplotype1"/>, position 2 is <see cref="Haplotype2"/>, matching the order the pairing loop
/// passed them to <c>PhenotypeInfo</c>'s two-source constructor.
/// </summary>
internal readonly record struct GenotypePair(int Haplotype1, int Haplotype2);

/// <summary>
/// A genotype's identity <i>for likelihood purposes</i>: the ids of its two haplotypes' HLA-name forms, by index into
/// <see cref="ExpandedGenotypes.HaplotypeNamesById"/>. Distinct from <see cref="GenotypePair"/>, which indexes the
/// survivors themselves and therefore still distinguishes typing category.
///
/// <para>
/// <b>This is the same equality as the genotype's own name form, not an approximation of it.</b> Ids are handed out per
/// <i>distinct</i> <c>LociInfo&lt;string&gt;</c>, so <c>Name1 == Name1' &amp;&amp; Name2 == Name2'</c> exactly when
/// haplotype 1's names match and haplotype 2's do. And two genotype name forms are equal exactly when that holds,
/// because <c>PhenotypeInfo(source1, source2)</c> puts <c>source1</c> at position 1 and <c>source2</c> at position 2 of
/// every locus, and <c>LocusInfo</c> equality is positional. So the collapse the pairing loop depends on - two
/// survivors differing only at an <i>excluded</i> locus share a name form, so their genotypes must occupy one
/// dictionary slot - holds by construction here rather than by care.
/// </para>
///
/// <para>
/// Ordered, therefore, and deliberately: <c>(a, b)</c> is not <c>(b, a)</c> unless the two name forms are equal, which
/// is what the name form itself also says. The pairing loop only ever emits <c>j &gt;= i</c>, so the transposed key is
/// never generated in the first place.
/// </para>
/// </summary>
internal readonly record struct GenotypeNameKey(int Name1, int Name2);

internal class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
{
    private readonly ICompressedPhenotypeConverter converter;
    private readonly IHaplotypeFrequencyService haplotypeFrequencyService;

    public CompressedPhenotypeExpander(
        ICompressedPhenotypeConverter converter,
        IHaplotypeFrequencyService haplotypeFrequencyService)
    {
        this.converter = converter;
        this.haplotypeFrequencyService = haplotypeFrequencyService;
    }

    /// <summary>
    /// The phenotype is converted to the typing categories that will be <b>read</b>, and no others. Converting all
    /// three costs up to 3 x 5 loci x 2 positions = 30 HMD lookups per donor, and most of them can never be read.
    ///
    /// <para>
    /// Two separate reasons the other two categories are dead work, and they are not equally safe:
    /// <list type="number">
    /// <item>
    /// <b>The short circuit reads SmallGGroup alone.</b> <see cref="IsUnambiguousAtAllowedLoci"/> and
    /// <see cref="BuildSingleSmallGGenotype"/> touch nothing else, so for the donors that return there the GGroup and
    /// PGroup conversions could never have been read. Deferring them is laziness, not a change of behaviour.
    /// </item>
    /// <item>
    /// <b>A category the set holds no haplotypes in cannot affect the expansion.</b> The pool yields an empty array for
    /// such a category and <c>CollectSurvivors</c> returns on an empty array, so no survivor can carry that category and
    /// nothing ever reads its groups. This one <i>does</i> change which lookups happen, so the category is read from the
    /// set rather than assumed - a future GGroup or PGroup import must still work.
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// The pool is therefore fetched <b>before</b> the second conversion. That reorders nothing that matters - the
    /// categories convert independently of each other and the survivor order is set by <c>CollectSurvivors</c>' fixed
    /// visit order - but it does put the "no haplotypes for set" throw ahead of those conversions, and it leaves the
    /// unambiguous branch's property intact: those donors never resolve the set.
    /// </para>
    /// </summary>
    public async Task<ExpandedGenotypes> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input)
    {
        var allowedLoci = input.MatchPredictionParameters.AllowedLoci;

        var groupsPerPosition = new DataByResolution<PossibleGroupsPerPosition>
        {
            SmallGGroup = await converter.ConvertPhenotype(input, HaplotypeTypingCategory.SmallGGroup)
        };

        if (IsUnambiguousAtAllowedLoci(allowedLoci, groupsPerPosition))
        {
            // The majority of donors: unambiguous at every allowed locus, so they never touch the pool.
            return BuildSingleSmallGGenotype(groupsPerPosition);
        }

        var (pool, interner, alleleIndex) = await FetchHaplotypesGroupedByTypingCategory(input.HfSetId);

        // SmallGGroup is already converted whether the set holds it or not, because the short circuit above needed it.
        // The other two are converted only if some pooled haplotype is typed at that category.
        if (pool.GGroup.Length > 0)
        {
            groupsPerPosition.GGroup = await converter.ConvertPhenotype(input, HaplotypeTypingCategory.GGroup);
        }

        if (pool.PGroup.Length > 0)
        {
            groupsPerPosition.PGroup = await converter.ConvertPhenotype(input, HaplotypeTypingCategory.PGroup);
        }

        return await ExpandToPotentialDiplotypes(input.HfSetId, allowedLoci, groupsPerPosition, pool, interner, alleleIndex);
    }

    private static ExpandedGenotypes BuildSingleSmallGGenotype(DataByResolution<PossibleGroupsPerPosition> groupsPerPosition)
    {
        // The one certain genotype is still a pair - of the subject's own two positions rather than of two pool
        // haplotypes - so it goes down the same (haplotypes, pair) road as the expanded path, and every consumer sees
        // one shape.
        var position1 = SingleGroupPerLocus(groupsPerPosition, LocusPosition.One);
        var position2 = SingleGroupPerLocus(groupsPerPosition, LocusPosition.Two);

        // The same interning the expanded path uses, for the same reason it is safe to share: a homozygous subject's two
        // positions have equal name forms and so collapse to one id, which is exactly what the one-entry likelihood
        // dictionary below already said.
        var (nameIdByPosition, haplotypeNamesById) = InternHaplotypeNames([position1, position2]);
        var nameKey = new GenotypeNameKey(nameIdByPosition[0], nameIdByPosition[1]);

        // No frequency is resolved on this path and none is needed: one genotype is already certain, so
        // GenotypeImputationService replaces this placeholder with a likelihood of 1.
        return new ExpandedGenotypes(
            [position1, position2],
            [new GenotypePair(0, 1)],
            [nameKey],
            new Dictionary<GenotypeNameKey, decimal> { [nameKey] = 0m },
            haplotypeNamesById);
    }

    /// <summary>
    /// The one SmallGGroup group each locus holds at <paramref name="position"/>, as a haplotype. Only reachable
    /// behind <see cref="IsUnambiguousAtAllowedLoci"/>, which is what makes <c>Single()</c> safe.
    /// </summary>
    private static LociInfo<HlaAtKnownTypingCategory> SingleGroupPerLocus(
        DataByResolution<PossibleGroupsPerPosition> groupsPerPosition,
        LocusPosition position)
    {
        return groupsPerPosition.SmallGGroup.ToLociInfo((_, atPosition1, atPosition2) =>
        {
            var groups = position == LocusPosition.One ? atPosition1 : atPosition2;

            return groups == null ? null : new HlaAtKnownTypingCategory(groups.Single(), HaplotypeTypingCategory.SmallGGroup);
        });
    }

    private static bool IsUnambiguousAtAllowedLoci(
        ISet<Locus> allowedLoci,
        DataByResolution<PossibleGroupsPerPosition> groupsPerPosition)
    {
        return allowedLoci.All(l =>
        {
            var groupsAtLocus = groupsPerPosition.SmallGGroup.GetLocus(l);
            return groupsAtLocus.Position1?.Count == 1 && groupsAtLocus.Position2?.Count == 1;
        });
    }

    /// <summary>
    /// Filters a collection of haplotypes down to only those which are possible for an input phenotype, and then combines them into potential genotypes.
    /// </summary>
    /// <param name="hfSetId">Id of haplotype frequency set</param>
    /// <param name="allowedLoci">List of loci that are being considered.</param>
    /// <param name="groupsPerPosition">
    /// Allele groups present in the phenotype being expanded, for the categories the set holds - null for the others.
    /// </param>
    /// <param name="pool">The set's haplotypes as interned keys, grouped by typing category.</param>
    /// <param name="interner">The interner <paramref name="pool"/>'s ids belong to.</param>
    /// <param name="alleleIndex">Per (category, locus, allele id), the ascending <paramref name="pool"/> positions of haplotypes carrying it.</param>
    /// <returns>Set of diplotypes (pairs of haplotypes) which are possible for an input phenotype</returns>
    private async Task<ExpandedGenotypes> ExpandToPotentialDiplotypes(
        int hfSetId,
        ISet<Locus> allowedLoci,
        DataByResolution<PossibleGroupsPerPosition> groupsPerPosition,
        DataByResolution<HaplotypeKey[]> pool,
        HaplotypeInterner interner,
        DataByResolution<LociInfo<int[][]>> alleleIndex)
    {
        var haplotypes = GetHaplotypesForAllowedLoci(pool, interner, allowedLoci, groupsPerPosition, alleleIndex);
        var haplotypeList = haplotypes.ToList();

        // Materialise the allowed loci once: iterating the ISet directly inside the per-diplotype loop would box an enumerator on every pair.
        var allowedLociArray = allowedLoci.ToArray();

        // This selects WHICH frequency a haplotype has - with loci excluded, a haplotype stands for a group of stored
        // haplotypes whose frequencies are consolidated - and it is constant for the whole request. Hoisted because
        // LocusSettings.MatchPredictionLoci is an expression-bodied property that re-enumerates the enum and allocates
        // a fresh HashSet on every read.
        var excludedLoci = LocusSettings.MatchPredictionLoci.Except(allowedLoci).ToHashSet();

        // One resolution per survivor.
        var frequencies = await ResolveHaplotypeFrequencies(hfSetId, haplotypeList, excludedLoci);

        bool IsRepresentedInTargetPhenotype(HlaAtKnownTypingCategory hla, Locus locus, LocusPosition position)
        {
            // GetByCategory cannot be null here, however few categories were converted: a survivor's category is the
            // category of the pool array it came from, and an unconverted category's array is empty by construction.
            var groups = groupsPerPosition.GetByCategory(hla.TypingCategory).GetPosition(locus, position);
            return groups == null || groups.Contains(hla.Hla);
        }

        // The name form of each survivor, once, then one id per DISTINCT name form, so the pairing loop can key a
        // genotype's likelihood on eight bytes rather than on seven heap objects. There are only S survivors, against
        // up to 1.65M kept pairs, so this is the cheap end to pay at.
        var (nameIdBySurvivor, haplotypeNamesById) = InternHaplotypeNames(haplotypeList);

        // A List, not a HashSet - see the ExpandedGenotypes remarks for why this can never de-duplicate.
        // genotypeNameKeys is kept index-aligned with it so truncation needs no name form per pre-truncation genotype.
        //
        // These three are left to grow, deliberately. They hold one entry per KEPT pair, and no capacity available
        // here is the right one:
        //   - haplotypeList.Count is the SURVIVOR count. The kept-pair count is a fraction of S(S+1)/2, so at a large
        //     S this hint is smaller than the answer by orders of magnitude and removes almost no growth.
        //   - S(S+1)/2 itself is a correct upper bound and a useless one: it over-allocates by roughly ten times at
        //     every S, which for a large subject is worse than the growth it avoids.
        //   - The exact count needs the mask test, which is the loop below. Running it twice - once to count, once to
        //     fill - would size all three exactly, at the cost of a second O(S^2) pass.
        // The last of those is the only real candidate, and whether it wins depends on the growth cost it removes
        // against the pass it adds. Neither has been measured here, so it is not being guessed at.
        //
        // Note also that Dictionary growth does NOT re-hash these keys expensively: a GenotypeNameKey is two ints, so a
        // resize re-hashes a pair of integers per entry.
        var genotypePairs = new List<GenotypePair>();
        var genotypeNameKeys = new List<GenotypeNameKey>();
        var likelihoods = new Dictionary<GenotypeNameKey, decimal>();

        // Only keep diplotypes where, at every allowed locus, both haplotypes' HLA are represented within the target
        // phenotype (in either phase). This is the O(n^2) hot path, so it is written as an explicit loop to avoid the
        // per-pair delegate and throwaway-collection allocations that the functional combinators
        // (Combinations.AllPairs / LociInfo.AllAtLoci) would otherwise incur on every pair. The mask build is hoisted
        // out of it because it IS the pair test, resolved once per survivor rather than once per pair.
        var representationMasks = BuildRepresentationMasks(haplotypeList, allowedLociArray, IsRepresentedInTargetPhenotype);

        // Every allowed locus must be represented, so the target is a full set of low bits rather than "non-zero".
        var positionShift = allowedLociArray.Length;
        var allLociRepresented = (1 << positionShift) - 1;

        for (var i = 0; i < haplotypeList.Count; i++)
        {
            var mask1 = representationMasks[i];

            // Start at i (not i + 1) to include the self-pair, matching Combinations.AllPairs(..., shouldIncludeSelfPairs: true).
            for (var j = i; j < haplotypeList.Count; j++)
            {
                var mask2 = representationMasks[j];

                // Position 1 of haplotype 1 against position 2 of haplotype 2, and the same the other way round -
                // the two phases per locus, for every allowed locus at once. Shifting one operand down by
                // positionShift lines its position-2 lane up with the other's position-1 lane.
                var represented = (mask1 & (mask2 >> positionShift)) | ((mask1 >> positionShift) & mask2);

                if ((represented & allLociRepresented) == allLociRepresented)
                {
                    // Read only for a pair that survives - the large majority of pairs do not.
                    var names1 = haplotypeNamesById[nameIdBySurvivor[i]];
                    var names2 = haplotypeNamesById[nameIdBySurvivor[j]];

                    // Eight bytes, and no PhenotypeInfo: a name form here would be seven objects for every kept pair,
                    // and truncation keeps a small fraction of them.
                    var nameKey = new GenotypeNameKey(nameIdBySurvivor[i], nameIdBySurvivor[j]);

                    // Appended together, on purpose adjacent: the two lists are read by index in lockstep
                    // downstream, so anything that adds to one without the other silently mis-pairs a genotype with
                    // another genotype's likelihood.
                    genotypePairs.Add(new GenotypePair(i, j));
                    genotypeNameKeys.Add(nameKey);

                    // The multiplication order is fixed - position 1's frequency, then position 2's, then the
                    // correction. decimal multiplication carries scale, so re-ordering these could shift the result
                    // in the last digits, and these likelihoods are compared for exact equality.
                    //
                    // An indexer assignment, not Add: two survivors can share HLA names while differing in typing
                    // category (they differ only at an excluded locus), so distinct genotypes can collapse to one
                    // key here. Their likelihoods are then necessarily equal, because a frequency is keyed on the
                    // names - so which write lands does not matter, but throwing would. Keying on the name ids rather
                    // than on the name form does not change which keys collapse: sharing a name form is sharing an id.
                    likelihoods[nameKey] =
                        frequencies[i] * frequencies[j] * HomozygosityCorrectionFactor(names1, names2, allowedLociArray);
                }
            }
        }

        return new ExpandedGenotypes(haplotypeList, genotypePairs, genotypeNameKeys, likelihoods, haplotypeNamesById);
    }

    /// <summary>
    /// One HLA-name form per survivor, then one id per <b>distinct</b> name form.
    /// </summary>
    /// <remarks>
    /// Survivors that differ only in typing category, or only at a locus this key excludes, have equal name forms and
    /// therefore share an id - which is precisely the collapse <see cref="ExpandedGenotypes.Likelihoods"/> performs,
    /// moved from the genotype to the haplotype. The dictionary is over S entries, paid once, against the up-to-1.65M
    /// kept pairs that would otherwise build a <c>PhenotypeInfo</c> each.
    /// </remarks>
    private static (int[] NameIdBySurvivor, IReadOnlyList<HfSetHaplotypeNames> HaplotypeNamesById) InternHaplotypeNames(
        IReadOnlyList<LociInfo<HlaAtKnownTypingCategory>> survivors)
    {
        var idByName = new Dictionary<HfSetHaplotypeNames, int>(survivors.Count);
        var nameIdBySurvivor = new int[survivors.Count];
        var haplotypeNamesById = new List<HfSetHaplotypeNames>(survivors.Count);

        for (var h = 0; h < survivors.Count; h++)
        {
            var names = survivors[h].Map(hla => hla?.Hla);

            if (!idByName.TryGetValue(names, out var id))
            {
                id = haplotypeNamesById.Count;
                idByName[names] = id;
                haplotypeNamesById.Add(names);
            }

            nameIdBySurvivor[h] = id;
        }

        return (nameIdBySurvivor, haplotypeNamesById);
    }

    /// <summary>
    /// Which loci and positions of the target phenotype a survivor can occupy, as one integer per survivor.
    ///
    /// <para>
    /// <c>IsRepresentedInTargetPhenotype(hla, locus, position)</c> reads <b>only</b> the one haplotype - it never looks
    /// at the other haplotype of the pair - so its value is a property of the survivor alone, and belongs outside the
    /// O(S²) pairing loop. Resolving it S times here turns the pair test into three integer operations that touch no
    /// string and allocate nothing.
    /// </para>
    ///
    /// <para>
    /// Bit <c>l</c> is position 1 at <c>allowedLoci[l]</c>; bit <c>l + allowedLoci.Length</c> is position 2 at the same
    /// locus. Two lanes of one <c>int</c> rather than two arrays, so the pairing loop reads one array element per
    /// iteration and the whole mask array stays in cache across the inner loop.
    /// </para>
    ///
    /// <para>
    /// <b>The predicate is not re-implemented here</b> - the caller's own local function is passed in and called with
    /// the same arguments, so the mask cannot disagree with it. Every (locus, position) is evaluated for every
    /// survivor, where a pair loop's <c>&amp;&amp;</c> chain could stop early; the predicate is pure, so this changes
    /// cost and nothing else.
    /// </para>
    /// </summary>
    private static int[] BuildRepresentationMasks(
        List<LociInfo<HlaAtKnownTypingCategory>> haplotypes,
        Locus[] allowedLoci,
        Func<HlaAtKnownTypingCategory, Locus, LocusPosition, bool> isRepresentedInTargetPhenotype)
    {
        var masks = new int[haplotypes.Count];

        for (var h = 0; h < haplotypes.Count; h++)
        {
            var haplotype = haplotypes[h];
            var mask = 0;

            for (var l = 0; l < allowedLoci.Length; l++)
            {
                var locus = allowedLoci[l];
                var hla = haplotype.GetLocus(locus);

                if (isRepresentedInTargetPhenotype(hla, locus, LocusPosition.One))
                {
                    mask |= 1 << l;
                }

                if (isRepresentedInTargetPhenotype(hla, locus, LocusPosition.Two))
                {
                    mask |= 1 << (l + allowedLoci.Length);
                }
            }

            masks[h] = mask;
        }

        return masks;
    }

    /// <summary>
    /// One frequency per survivor, through the unchanged <see cref="IHaplotypeFrequencyService.GetFrequencyForHla"/>.
    ///
    /// <para>
    /// Calling the same method with the same arguments is the point: it keeps the
    /// direct → consolidated-warm → consolidated-cold cascade, and with it the fact that on any key with excluded loci
    /// a survivor's frequency is the <b>sum</b> over the stored haplotypes it collapsed - not any individual stored
    /// frequency. That is the majority of a set's rows, and the reason this memoises the lookup rather than
    /// reimplementing it or carrying a value off the pool.
    /// </para>
    ///
    /// <para>
    /// It resolves for every survivor, including any that no kept pair ends up using - so a donor whose typing is not
    /// representable at all pays S lookups and keeps nothing. Accepted rather than overlooked: filling the array
    /// lazily would put a branch on the O(S²) pairing path, which is the worse trade, and such donors are a small
    /// fraction of the corpus with a small S.
    /// </para>
    /// </summary>
    private async Task<decimal[]> ResolveHaplotypeFrequencies(
        int hfSetId,
        List<LociInfo<HlaAtKnownTypingCategory>> survivors,
        ISet<Locus> excludedLoci)
    {
        var frequencies = new decimal[survivors.Count];

        for (var i = 0; i < survivors.Count; i++)
        {
            frequencies[i] = await haplotypeFrequencyService.GetFrequencyForHla(
                hfSetId, survivors[i].Map(hla => hla?.Hla), excludedLoci);
        }

        return frequencies;
    }

    /// <summary>
    /// 1 when the genotype is homozygous at every allowed locus, else 2.
    ///
    /// <para>
    /// This is the same heterozygosity test as <c>UnambiguousGenotypeExpander.GetHeterozygousLoci</c>, which serves the
    /// debug likelihood endpoint: a genotype is heterozygous at a locus exactly when its two haplotypes differ there,
    /// so the two ask one question in two shapes. They are not shared because they need different answers - that one
    /// returns the <c>List&lt;Locus&gt;</c> it uses to enumerate phase permutations, while this one needs only whether
    /// the list would be empty, and runs once per kept pair, so it must allocate nothing.
    /// </para>
    ///
    /// <para>
    /// Neither is pinned to the other; both are pinned to hand-computed likelihoods, which is what stops them drifting
    /// apart unnoticed. This side is covered by <c>ImputationEquivalenceTests</c> (one test asserts a x1, a x2 and a x1
    /// case as exact decimals), the other by <c>UnambiguousGenotypeExpanderTests</c> and
    /// <c>LikelihoodCalculatorTests</c>. A change to either that alters the rule fails its own suite.
    /// </para>
    ///
    /// <para>
    /// It takes the two haplotypes in their name form, which is the form it compares. A stored haplotype may be
    /// untyped at an allowed locus, so a name may be null, and two nulls compare equal.
    /// </para>
    /// </summary>
    private static int HomozygosityCorrectionFactor(
        HfSetHaplotypeNames haplotype1,
        HfSetHaplotypeNames haplotype2,
        Locus[] allowedLoci)
    {
        foreach (var locus in allowedLoci)
        {
            if (haplotype1.GetLocus(locus) != haplotype2.GetLocus(locus))
            {
                return 2;
            }
        }

        return 1;
    }

    /// <summary>
    /// The pooled haplotypes the subject's own allele groups can explain, in pool order.
    ///
    /// <para>
    /// Every pooled haplotype used to be tested at every allowed locus, and the large majority failed. Instead, the
    /// subject's admitted allele ids at each allowed locus are looked up in <paramref name="alleleIndex"/> - the
    /// pool's own per-(locus, allele id) position lists - and the allowed locus with the fewest candidate positions
    /// seeds the scan: every true survivor must appear among that locus's candidates too, since it has to pass that
    /// locus's mask like every other allowed locus, so filtering only those candidates can never miss a survivor.
    /// See <see cref="SelectCandidatePositions"/>.
    /// </para>
    ///
    /// <para>
    /// The subject's groups are resolved into the set's own id space once per (category, locus), into a
    /// <c>bool[]</c> indexed by allele id - dense, because <c>AlleleInterner</c> mints ids from 0 - and the per
    /// haplotype test becomes an array read. Not even a <c>HashSet&lt;int&gt;</c>: nothing is hashed at all.
    /// </para>
    ///
    /// <para>
    /// <b>Ids stop here.</b> They mean nothing outside <c>entry.Interner</c>, and a later fetch of the same set id can
    /// return a different entry with a different id space, so survivors are resolved back to names before they leave
    /// this method - which is also the form <c>GetFrequencyForHla</c> needs, since it re-enters the cache.
    /// </para>
    /// </summary>
    private static IEnumerable<LociInfo<HlaAtKnownTypingCategory>> GetHaplotypesForAllowedLoci(
        DataByResolution<HaplotypeKey[]> pool,
        HaplotypeInterner interner,
        ISet<Locus> allowedLoci,
        DataByResolution<PossibleGroupsPerPosition> groupsPerPosition,
        DataByResolution<LociInfo<int[][]>> alleleIndex)
    {
        // The fetch happens in the caller, which needs the pool to decide which conversions to make.
        var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);
        var allowedLociArray = allowedLoci.ToArray();

        // Insertion order is the survivor order, which is the pairing order, which is what the truncater's tie-break
        // reads - so the three categories are visited in a fixed order, and each pool array is in the order
        // ProjectPool produced. Nothing here may reorder.
        var survivors = new HashSet<LociInfo<HlaAtKnownTypingCategory>>();

        CollectSurvivors(HaplotypeTypingCategory.GGroup, pool.GGroup, alleleIndex.GGroup);
        CollectSurvivors(HaplotypeTypingCategory.PGroup, pool.PGroup, alleleIndex.PGroup);
        CollectSurvivors(HaplotypeTypingCategory.SmallGGroup, pool.SmallGGroup, alleleIndex.SmallGGroup);

        return survivors;

        void CollectSurvivors(HaplotypeTypingCategory category, HaplotypeKey[] haplotypes, LociInfo<int[][]> indexForCategory)
        {
            if (haplotypes.Length == 0)
            {
                return;
            }

            var (allowedAlleles, admittedIds) = BuildAllowedAlleleMasks(interner, groupsPerLocus.GetByCategory(category), allowedLociArray);
            var candidates = SelectCandidatePositions(indexForCategory, allowedLociArray, admittedIds, haplotypes.Length);

            foreach (var position in candidates)
            {
                var haplotype = haplotypes[position];

                if (!IsExplicableBySubject(haplotype, allowedAlleles, allowedLociArray))
                {
                    continue;
                }

                // Only now is a name needed, and only for a survivor - a small fraction of a pool that can hold
                // hundreds of thousands of haplotypes. Attaching the typing category and nulling the excluded loci
                // are both folded into this one Map.
                var names = interner.ReverseLookup(haplotype);

                survivors.Add(names.Map((locus, hla) =>
                    allowedLoci.Contains(locus) ? new HlaAtKnownTypingCategory(hla, category) : null));
            }
        }
    }

    /// <summary>
    /// The pool positions <see cref="IsExplicableBySubject"/> could possibly accept, in pool order - a superset of
    /// the true survivors, cheap to compute from <paramref name="alleleIndex"/>.
    ///
    /// <para>
    /// Picks the allowed locus whose admitted allele ids cover the fewest pool positions between them, and returns
    /// the (ascending, since each index bucket already is) merge of just those buckets. A haplotype cannot survive
    /// <see cref="IsExplicableBySubject"/> without passing every allowed locus's mask, including this one, so every
    /// true survivor is necessarily among these candidates - the caller still runs the full, unmodified
    /// <see cref="IsExplicableBySubject"/> check over them, this only skips positions that check could never accept.
    /// </para>
    ///
    /// <para>
    /// A locus the subject is untyped at (<c>admittedIds[l] == null</c>) admits every haplotype and so cannot narrow
    /// anything - it is skipped as a seed. If every allowed locus is like that, nothing can narrow the pool at all,
    /// and this falls back to the full pool in order, exactly as before this index existed.
    /// </para>
    /// </summary>
    private static IEnumerable<int> SelectCandidatePositions(
        LociInfo<int[][]> alleleIndex, Locus[] allowedLoci, int[][] admittedIds, int poolSize)
    {
        var bestLocus = -1;
        var bestCount = 0;

        for (var l = 0; l < allowedLoci.Length; l++)
        {
            var ids = admittedIds[l];

            if (ids == null)
            {
                continue;
            }

            var buckets = alleleIndex.GetLocus(allowedLoci[l]);
            var count = 0;

            foreach (var id in ids)
            {
                count += buckets[id].Length;
            }

            if (bestLocus == -1 || count < bestCount)
            {
                bestLocus = l;
                bestCount = count;
            }
        }

        if (bestLocus == -1)
        {
            return Enumerable.Range(0, poolSize);
        }

        var winningIds = admittedIds[bestLocus];
        var winningBuckets = alleleIndex.GetLocus(allowedLoci[bestLocus]);

        if (winningIds.Length == 1)
        {
            // The common case - a subject typed to a single allele group at this locus - needs no merge at all: the
            // one bucket is already the candidate list, in pool order.
            return winningBuckets[winningIds[0]];
        }

        // A handful of small, individually-ascending buckets (one per admitted id - and an id is admitted by at most
        // one bucket, so they share no positions) - concatenate and sort rather than a k-way merge, since the total
        // is already far smaller than the pool this replaced scanning in full.
        var merged = new List<int>(bestCount);

        foreach (var id in winningIds)
        {
            merged.AddRange(winningBuckets[id]);
        }

        merged.Sort();
        return merged;
    }

    /// <summary>
    /// Which allele ids the subject's groups admit, per allowed locus: <c>masks[l][id]</c> / <c>admittedIds[l]</c>,
    /// or both null at position <c>l</c> where the subject has no groups at that locus and therefore admits
    /// everything - the <c>hlaGroups == null</c> branch.
    ///
    /// <para>
    /// An allele the set has never seen resolves to <see cref="AlleleInterner.NotFound"/> and is simply not marked, so
    /// it can match nothing. A null or empty group name resolves to 0, the id of an untyped locus, so it matches an
    /// untyped pooled haplotype; the storage layer conflates null with the empty string when it interns a set, so
    /// neither this nor the frozen dictionary it derives from can tell the two apart.
    /// </para>
    ///
    /// <para>
    /// <paramref name="allowedLoci"/>[l]'s admitted ids are collected alongside its mask, in the same pass, for
    /// <see cref="SelectCandidatePositions"/> to look up in the pool's allele index - there are normally very few of
    /// them (one, or a handful for an ambiguous/MAC-expanded group), well short of needing anything but a small list.
    /// </para>
    /// </summary>
    private static (bool[][] Masks, int[][] AdmittedIds) BuildAllowedAlleleMasks(
        HaplotypeInterner interner,
        LociInfo<ISet<string>> groupsPerLocus,
        Locus[] allowedLoci)
    {
        var masks = new bool[allowedLoci.Length][];
        var admittedIds = new int[allowedLoci.Length][];

        for (var l = 0; l < allowedLoci.Length; l++)
        {
            var groups = groupsPerLocus.GetLocus(allowedLoci[l]);

            if (groups == null)
            {
                continue;
            }

            var alleles = interner.ForLocus(allowedLoci[l]);
            var mask = new bool[alleles.IdCount];
            var ids = new List<int>(groups.Count);

            foreach (var group in groups)
            {
                var id = alleles.Resolve(group);

                if (id != AlleleInterner.NotFound)
                {
                    mask[id] = true;
                    ids.Add(id);
                }
            }

            masks[l] = mask;
            admittedIds[l] = ids.ToArray();
        }

        return (masks, admittedIds);
    }

    /// <summary>
    /// The innermost test of the pool filter: one array read per allowed locus. A null mask is the subject being
    /// untyped there, which admits every haplotype.
    /// </summary>
    private static bool IsExplicableBySubject(HaplotypeKey haplotype, bool[][] allowedAlleles, Locus[] allowedLoci)
    {
        for (var l = 0; l < allowedLoci.Length; l++)
        {
            var mask = allowedAlleles[l];

            if (mask != null && !mask[haplotype.GetLocus(allowedLoci[l])])
            {
                return false;
            }
        }

        return true;
    }

    private async Task<(DataByResolution<HaplotypeKey[]> Pool, HaplotypeInterner Interner, DataByResolution<LociInfo<int[][]>> AlleleIndex)>
        FetchHaplotypesGroupedByTypingCategory(int frequencySetId)
    {
        // This piece of code doesn't even need dictionary, it just needs typingCategory => List<Hla> mapping from it
        // Huge on the first touch of a set (a whole set out of SQL, then interned), ~0 on every subsequent donor.
        var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);

        if (haplotypeFrequencies.SetFrequencies.Count == 0)
        {
            throw new Exception($"No haplotypes could be found for set id {frequencySetId}.");
        }

        // Both projections live on the cache entry, which owns both of their inputs and has the per-set lifetime
        // they want. They are therefore paid by the first donor to touch a set, and are ~0 for every donor after it.
        //
        // The interner travels with the pool (and its index) because both are ids: reading them off the same entry
        // instance is what makes the ids meaningful.
        return (haplotypeFrequencies.ProjectedPool, haplotypeFrequencies.Interner, haplotypeFrequencies.AlleleIndex);
    }

    private static LociInfo<ISet<string>> CombineSetsAtLoci(PossibleGroupsPerPosition phenotypeInfo)
    {
        // Null for a category that was not converted, which is a category the set holds no haplotypes in. Its pool
        // array is empty, so CollectSurvivors returns before it would read this - the null goes nowhere.
        if (phenotypeInfo == null)
        {
            return null;
        }

        return phenotypeInfo.ToLociInfo((_, set1, set2) =>
            set1 != null && set2 != null
                ? (ISet<string>)new HashSet<string>(set1.Concat(set2))
                : null);
    }
}