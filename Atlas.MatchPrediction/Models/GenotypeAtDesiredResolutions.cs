// The two resolutions this type exists to hold, named. Both are PhenotypeInfo<string> and nothing but these aliases
// tells them apart.
//
//   HfSetGenotypeNames - names at the resolution the haplotype frequency set stores (per row: P group, or G group
//                        where a null allele meant no P group existed), typing category ERASED.
//   PGroupGenotype     - P group, or absent. Converted from the above, so a G group never survives to here.
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using PGroupGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Models;

/// <remarks>
/// Built through a constructor taking an <see cref="ImputedGenotype"/> rather than its <see cref="HaplotypeResolution"/>
/// and <see cref="GenotypeLikelihood"/> as independent arguments: both come from the one <c>ImputedGenotype</c>, whose
/// <c>Names</c>/<c>Genotype</c> pairing is already guaranteed correct upstream (see
/// <c>ExpandedGenotypes.MaterialiseNames</c>), so the two cannot be set from unrelated sources here. Re-deriving
/// <c>HaplotypeResolution</c> via <c>ToHlaNames()</c> on every call, as an earlier version of this type did, would
/// reintroduce the per-genotype cost <c>GenotypeConverter</c> exists to avoid - <c>ImputedGenotype.Names</c> is already
/// that derivation, computed once upstream.
/// </remarks>
public class GenotypeAtDesiredResolutions
{
    public GenotypeAtDesiredResolutions(ImputedGenotype genotype, PGroupGenotype stringMatchableResolution)
    {
        HaplotypeResolution = genotype.Names;
        GenotypeLikelihood = genotype.Likelihood;
        StringMatchableResolution = stringMatchableResolution;
    }

    /// <summary>
    /// HLA at the resolution at which they were stored.
    /// I.e. P group, or G group where a null allele meant no P group existed.
    ///
    /// <para>
    /// The direction matters, and it is easy to state backwards. A P group is defined on the ABD protein sequence and
    /// so <b>excludes</b> null alleles, which express no protein - so a haplotype carrying one has no P group at that
    /// locus. <c>FrequencySetImporter.ConvertHaplotypesToPGroupResolutionAndConsolidate</c> converts haplotype by
    /// haplotype and keeps the G group form for exactly those, which is why <c>HaplotypeFrequency.TypingCategory</c> is
    /// a per-row column and one set can hold both resolutions.
    /// </para>
    /// </summary>
    public HfSetGenotypeNames HaplotypeResolution { get; }

    /// <summary>
    /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
    ///
    /// <para>
    /// That resolution is <b>P group</b>, or absent - never a G group, whatever <see cref="HaplotypeResolution"/> above
    /// happened to be stored as. The property keeps its name because "string matchable" says why it exists; the alias
    /// says what it holds. <c>IMatchCalculationService.CalculateMatchCounts_Fast</c> carries the evidence.
    /// </para>
    /// </summary>
    public PGroupGenotype StringMatchableResolution { get; }

    /// <summary>
    /// Likelihood of this genotype.
    ///
    /// Stored with the genotype to avoid dictionary lookups when calculating final likelihoods, as looking up the same genotype multiple times
    /// for different patient/donor pairs is inefficient
    /// </summary>
    public decimal GenotypeLikelihood { get; }
}