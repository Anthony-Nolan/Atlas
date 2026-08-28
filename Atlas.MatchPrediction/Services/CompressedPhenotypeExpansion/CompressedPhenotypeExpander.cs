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

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;

internal class CompressedPhenotypeExpanderInput
{
    /// <summary>
    /// Given phenotype. Can be of any supported HLA resolution.
    /// </summary>
    public PhenotypeInfo<string> Phenotype { get; set; }

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
/// <see cref="Likelihoods"/> is keyed by the genotype's HLA <i>names</i>, matching
/// <c>ImputedGenotypes.GenotypeLikelihoods</c>, so its count is the DISTINCT genotype count while
/// <see cref="GenotypePairs"/> keeps typing category and so may hold more. Keying it here also spares one
/// <c>ToHlaNames()</c> per pre-truncation genotype downstream.
/// </para>
///
/// <para>
/// <see cref="GenotypeHlaNames"/> is <see cref="GenotypePairs"/> index for index, so
/// <c>ExpandedGenotypeTruncater</c> can test a genotype's membership of the kept key set without re-deriving its name
/// form. The pairing loop below has to build that name form anyway, to key the likelihood.
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
/// <b>A genotype is carried as the two pool indices it came from, and built only if it survives truncation.</b> A
/// <c>PhenotypeInfo&lt;T&gt;</c> is seven objects - itself and one <c>LocusInfo</c> per locus - and a capped donor
/// keeps 2,000 genotypes out of up to 1.65M pairs. The name form has to exist, because <see cref="Likelihoods"/> is
/// keyed by it and a collapsed key must occupy one slot rather than two; the category form does not, because nothing
/// reads it until <c>ExpandedGenotypeTruncater</c> has decided which keys survive. A <see cref="GenotypePair"/> is
/// eight bytes in a contiguous list and allocates nothing.
/// </para>
/// </summary>
internal readonly record struct ExpandedGenotypes(
    IReadOnlyList<LociInfo<HlaAtKnownTypingCategory>> Haplotypes,
    List<GenotypePair> GenotypePairs,
    List<PhenotypeInfo<string>> GenotypeHlaNames,
    Dictionary<PhenotypeInfo<string>, decimal> Likelihoods)
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
}

/// <summary>
/// A genotype as the two pool haplotypes it is a pair of, by index into <see cref="ExpandedGenotypes.Haplotypes"/>.
/// Position 1 is <see cref="Haplotype1"/>, position 2 is <see cref="Haplotype2"/>, matching the order the pairing loop
/// passed them to <c>PhenotypeInfo</c>'s two-source constructor.
/// </summary>
internal readonly record struct GenotypePair(int Haplotype1, int Haplotype2);

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

        var groupsPerPosition = new DataByResolution<PhenotypeInfo<ISet<string>>>
        {
            SmallGGroup = await converter.ConvertPhenotype(input, HaplotypeTypingCategory.SmallGGroup)
        };

        if (IsUnambiguousAtAllowedLoci(allowedLoci, groupsPerPosition))
        {
            // The majority of donors: unambiguous at every allowed locus, so they never touch the pool.
            return BuildSingleSmallGGenotype(groupsPerPosition);
        }

        var (pool, interner) = await FetchHaplotypesGroupedByTypingCategory(input.HfSetId);

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

        return await ExpandToPotentialDiplotypes(input.HfSetId, allowedLoci, groupsPerPosition, pool, interner);
    }

    private static ExpandedGenotypes BuildSingleSmallGGenotype(DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
    {
        // The one certain genotype is still a pair - of the subject's own two positions rather than of two pool
        // haplotypes - so it goes down the same (haplotypes, pair) road as the expanded path, and every consumer sees
        // one shape.
        var position1 = SingleGroupPerLocus(groupsPerPosition, LocusPosition.One);
        var position2 = SingleGroupPerLocus(groupsPerPosition, LocusPosition.Two);

        var hlaNames = new PhenotypeInfo<string>(position1.Map(hla => hla?.Hla), position2.Map(hla => hla?.Hla));

        // No frequency is resolved on this path and none is needed: one genotype is already certain, so
        // GenotypeImputationService replaces this placeholder with a likelihood of 1.
        return new ExpandedGenotypes(
            [position1, position2],
            [new GenotypePair(0, 1)],
            [hlaNames],
            new Dictionary<PhenotypeInfo<string>, decimal> { [hlaNames] = 0m });
    }

    /// <summary>
    /// The one SmallGGroup group each locus holds at <paramref name="position"/>, as a haplotype. Only reachable
    /// behind <see cref="IsUnambiguousAtAllowedLoci"/>, which is what makes <c>Single()</c> safe.
    /// </summary>
    private static LociInfo<HlaAtKnownTypingCategory> SingleGroupPerLocus(
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition,
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
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
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
    /// <returns>Set of diplotypes (pairs of haplotypes) which are possible for an input phenotype</returns>
    private async Task<ExpandedGenotypes> ExpandToPotentialDiplotypes(
        int hfSetId,
        ISet<Locus> allowedLoci,
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition,
        DataByResolution<HaplotypeKey[]> pool,
        HaplotypeInterner interner)
    {
        var haplotypes = GetHaplotypesForAllowedLoci(pool, interner, allowedLoci, groupsPerPosition);
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

        // The name form of each survivor, once. The pairing loop then builds a genotype's name form - the one
        // PhenotypeInfo it cannot avoid, because it keys the likelihood - straight from two of these, rather than
        // building the category form first and mapping it.
        var haplotypeNames = new LociInfo<string>[haplotypeList.Count];
        for (var h = 0; h < haplotypeList.Count; h++)
        {
            haplotypeNames[h] = haplotypeList[h].Map(hla => hla?.Hla);
        }

        // A List, not a HashSet - see the ExpandedGenotypes remarks for why this can never de-duplicate.
        // genotypeHlaNames is kept index-aligned with it so truncation needs no second ToHlaNames() per genotype.
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
        // Note also that Dictionary growth does NOT re-hash these keys expensively: LociInfo precomputes its hash in
        // its constructor and GetHashCode returns the cached value, so a resize re-reads an int per entry.
        var genotypePairs = new List<GenotypePair>();
        var genotypeHlaNames = new List<PhenotypeInfo<string>>();
        var likelihoods = new Dictionary<PhenotypeInfo<string>, decimal>();

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
                    var names1 = haplotypeNames[i];
                    var names2 = haplotypeNames[j];

                    var hlaNames = new PhenotypeInfo<string>(names1, names2);

                    // Appended together, on purpose adjacent: the two lists are read by index in lockstep
                    // downstream, so anything that adds to one without the other silently mis-pairs a genotype with
                    // another genotype's likelihood.
                    genotypePairs.Add(new GenotypePair(i, j));
                    genotypeHlaNames.Add(hlaNames);

                    // The multiplication order is fixed - position 1's frequency, then position 2's, then the
                    // correction. decimal multiplication carries scale, so re-ordering these could shift the result
                    // in the last digits, and these likelihoods are compared for exact equality.
                    //
                    // An indexer assignment, not Add: two survivors can share HLA names while differing in typing
                    // category (they differ only at an excluded locus), so distinct genotypes can collapse to one
                    // key here. Their likelihoods are then necessarily equal, because a frequency is keyed on the
                    // names - so which write lands does not matter, but throwing would.
                    likelihoods[hlaNames] =
                        frequencies[i] * frequencies[j] * HomozygosityCorrectionFactor(names1, names2, allowedLociArray);
                }
            }
        }

        return new ExpandedGenotypes(haplotypeList, genotypePairs, genotypeHlaNames, likelihoods);
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
        LociInfo<string> haplotype1,
        LociInfo<string> haplotype2,
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
    /// This is the hottest loop of the expansion: every pooled haplotype is tested at every allowed locus, and the
    /// large majority fail. The pool already holds the answer as an integer, because <c>SetFrequencies</c> is keyed by
    /// interned ids, so nothing here needs to hash an allele name.
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
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
    {
        // The fetch happens in the caller, which needs the pool to decide which conversions to make.
        var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);
        var allowedLociArray = allowedLoci.ToArray();

        // Insertion order is the survivor order, which is the pairing order, which is what the truncater's tie-break
        // reads - so the three categories are visited in a fixed order, and each pool array is in the order
        // ProjectPool produced. Nothing here may reorder.
        var survivors = new HashSet<LociInfo<HlaAtKnownTypingCategory>>();

        CollectSurvivors(HaplotypeTypingCategory.GGroup, pool.GGroup);
        CollectSurvivors(HaplotypeTypingCategory.PGroup, pool.PGroup);
        CollectSurvivors(HaplotypeTypingCategory.SmallGGroup, pool.SmallGGroup);

        return survivors;

        void CollectSurvivors(HaplotypeTypingCategory category, HaplotypeKey[] haplotypes)
        {
            if (haplotypes.Length == 0)
            {
                return;
            }

            var allowedAlleles = BuildAllowedAlleleMasks(interner, groupsPerLocus.GetByCategory(category), allowedLociArray);

            foreach (var haplotype in haplotypes)
            {
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
    /// Which allele ids the subject's groups admit, per allowed locus: <c>mask[l][id]</c>, or a null mask where the
    /// subject has no groups at that locus and therefore admits everything - the <c>hlaGroups == null</c> branch.
    ///
    /// <para>
    /// An allele the set has never seen resolves to <see cref="AlleleInterner.NotFound"/> and is simply not marked, so
    /// it can match nothing. A null or empty group name resolves to 0, the id of an untyped locus, so it matches an
    /// untyped pooled haplotype; the storage layer conflates null with the empty string when it interns a set, so
    /// neither this nor the frozen dictionary it derives from can tell the two apart.
    /// </para>
    /// </summary>
    private static bool[][] BuildAllowedAlleleMasks(
        HaplotypeInterner interner,
        LociInfo<ISet<string>> groupsPerLocus,
        Locus[] allowedLoci)
    {
        var masks = new bool[allowedLoci.Length][];

        for (var l = 0; l < allowedLoci.Length; l++)
        {
            var groups = groupsPerLocus.GetLocus(allowedLoci[l]);

            if (groups == null)
            {
                continue;
            }

            var alleles = interner.ForLocus(allowedLoci[l]);
            var mask = new bool[alleles.IdCount];

            foreach (var group in groups)
            {
                var id = alleles.Resolve(group);

                if (id != AlleleInterner.NotFound)
                {
                    mask[id] = true;
                }
            }

            masks[l] = mask;
        }

        return masks;
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

    private async Task<(DataByResolution<HaplotypeKey[]> Pool, HaplotypeInterner Interner)> FetchHaplotypesGroupedByTypingCategory(
        int frequencySetId)
    {
        // This piece of code doesn't even need dictionary, it just needs typingCategory => List<Hla> mapping from it
        // Huge on the first touch of a set (a whole set out of SQL, then interned), ~0 on every subsequent donor.
        var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);

        if (haplotypeFrequencies.SetFrequencies.Count == 0)
        {
            throw new Exception($"No haplotypes could be found for set id {frequencySetId}.");
        }

        // The projection lives on the cache entry, which owns both of its inputs and has the per-set lifetime it
        // wants. It is therefore paid by the first donor to touch a set, and is ~0 for every donor after it.
        //
        // The interner travels with the pool because the pool is ids: they are two halves of one value, and reading
        // them off the same entry instance is what makes the ids meaningful.
        return (haplotypeFrequencies.ProjectedPool, haplotypeFrequencies.Interner);
    }

    private static LociInfo<ISet<string>> CombineSetsAtLoci(PhenotypeInfo<ISet<string>> phenotypeInfo)
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