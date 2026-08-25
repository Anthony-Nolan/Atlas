using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
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
    public Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input);
}

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

    public async Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input)
    {
        var allowedLoci = input.MatchPredictionParameters.AllowedLoci;

        var groupsPerPosition = await converter.ConvertPhenotype(input);

        if (IsUnambiguousAtAllowedLoci(allowedLoci, groupsPerPosition))
        {
            // Measured at 59.58% of donors: unambiguous at every allowed locus, so they never touch the pool.
            return BuildSingleSmallGGenotype(groupsPerPosition);
        }

        return await ExpandToPotentialDiplotypes(input.HfSetId, allowedLoci, groupsPerPosition);
    }

    private static ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> BuildSingleSmallGGenotype(DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
    {
        return new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>
        {
            groupsPerPosition.SmallGGroup.Map((_, __, v) =>
                v == null ? null : new HlaAtKnownTypingCategory(v.Single(), HaplotypeTypingCategory.SmallGGroup))
        };
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
    /// <param name="groupsPerPosition">Allele groups present in the phenotype being expanded.</param>
    /// <returns>Set of diplotypes (pairs of haplotypes) which are possible for an input phenotype</returns>
    private async Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandToPotentialDiplotypes(
        int hfSetId,
        ISet<Locus> allowedLoci,
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
    {
        var haplotypes = await GetHaplotypesForAllowedLoci(hfSetId, allowedLoci, groupsPerPosition);
        var haplotypeList = haplotypes.ToList();

        // Materialise the allowed loci once: iterating the ISet directly inside the per-diplotype loop would box an enumerator on every pair.
        var allowedLociArray = allowedLoci.ToArray();

        bool IsRepresentedInTargetPhenotype(HlaAtKnownTypingCategory hla, Locus locus, LocusPosition position)
        {
            var groups = groupsPerPosition.GetByCategory(hla.TypingCategory).GetPosition(locus, position);
            return groups == null || groups.Contains(hla.Hla);
        }

        // Only keep diplotypes where, at every allowed locus, both haplotypes' HLA are represented within the target phenotype (in either phase).
        // This is the O(n^2) hot path, so it is written as an explicit loop to avoid the per-pair delegate and throwaway-collection allocations
        // that the functional combinators (Combinations.AllPairs / LociInfo.AllAtLoci) would otherwise incur on every pair.
        bool IsRepresentedDiplotype(LociInfo<HlaAtKnownTypingCategory> haplotype1, LociInfo<HlaAtKnownTypingCategory> haplotype2)
        {
            foreach (var locus in allowedLociArray)
            {
                var hla1 = haplotype1.GetLocus(locus);
                var hla2 = haplotype2.GetLocus(locus);

                var representedDirectly =
                    IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.One) &&
                    IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.Two);

                var representedInverted =
                    IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.Two) &&
                    IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.One);

                if (!representedDirectly && !representedInverted)
                {
                    return false;
                }
            }

            return true;
        }

        var diplotypes = new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>();

        for (var i = 0; i < haplotypeList.Count; i++)
        {
            // Start at i (not i + 1) to include the self-pair, matching Combinations.AllPairs(..., shouldIncludeSelfPairs: true).
            for (var j = i; j < haplotypeList.Count; j++)
            {
                var haplotype1 = haplotypeList[i];
                var haplotype2 = haplotypeList[j];
                if (IsRepresentedDiplotype(haplotype1, haplotype2))
                {
                    diplotypes.Add(new PhenotypeInfo<HlaAtKnownTypingCategory>(haplotype1, haplotype2));
                }
            }
        }

        return diplotypes;
    }

    /// <summary>
    /// The pooled haplotypes the subject's own allele groups can explain, in pool order.
    ///
    /// <para>
    /// ATL-233 T1 follow-up. This is the second-largest phase and 89.6% of it was one line: 531.6M
    /// <c>ISet&lt;string&gt;.Contains</c> calls across the corpus, at 29.0 ns each, of which <b>0.20% passed</b>. Each
    /// was a hash of a 7-15 character allele name, a bucket probe and an ordinal compare - and the pool already knew
    /// the answer as an integer, because <c>SetFrequencies</c> is keyed by interned ids.
    /// </para>
    ///
    /// <para>
    /// So the subject's groups are resolved into the set's own id space once per (category, locus), into a
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
    private async Task<IEnumerable<LociInfo<HlaAtKnownTypingCategory>>> GetHaplotypesForAllowedLoci(
        int frequencySetId,
        ISet<Locus> allowedLoci,
        DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
    {
        var (pool, interner) = await FetchHaplotypesGroupedByTypingCategory(frequencySetId);

        var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);

        var allowedLociArray = allowedLoci.ToArray();

        // Insertion order is the survivor order, which is the pairing order, which is what the truncater's
        // tie-break reads - so the three categories are visited in the order the shipped Concat produced, and each
        // pool array is in the order ProjectPool produced. Nothing here may reorder.
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

                // Only now is a name needed, and only for a survivor: S is 55.5 on average against an H of up to
                // 274,606. The two Maps the shipped code did per surviving haplotype - one to attach the category,
                // one to null the excluded loci - are folded into this one.
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
    /// it can match nothing - which is what <c>Contains</c> did with it. A null or empty group name resolves to 0, the
    /// id of an untyped locus, matching the shipped <c>Contains(null)</c> against an untyped pooled haplotype; the
    /// storage layer conflated null with the empty string when it interned the set, so this cannot tell them apart
    /// either, and neither could the frozen dictionary it is derived from.
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
    /// The 531.6M-times-per-corpus test, now one array read per allowed locus. A null mask is the subject being
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

        // ATL-233 T1: the projection this used to perform per donor now lives on the cache entry, which owns both of
        // its inputs and has the per-set lifetime it wants. It is therefore paid by the first donor to touch a set,
        // and is ~0 for every donor after it.
        //
        // The interner travels with the pool because the pool is now ids: they are two halves of one value, and
        // reading them off the same entry instance is what makes the ids meaningful.
        return (haplotypeFrequencies.ProjectedPool, haplotypeFrequencies.Interner);
    }

    private static LociInfo<ISet<string>> CombineSetsAtLoci(PhenotypeInfo<ISet<string>> phenotypeInfo)
    {
        return phenotypeInfo.ToLociInfo((_, set1, set2) => 
            set1 != null && set2 != null
                ? (ISet<string>)new HashSet<string>(set1.Concat(set2))
                : null);
    }
}