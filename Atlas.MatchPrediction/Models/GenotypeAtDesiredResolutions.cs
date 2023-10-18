using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Models;

public class GenotypeAtDesiredResolutions
{
    /// <summary>
    /// HLA at the resolution at which they were stored.
    /// i.e. G group, or P group if any null alleles are present in the haplotype.
    /// </summary>
    public PhenotypeInfo<string> HaplotypeResolution { get; }

    /// <summary>
    /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
    /// </summary>
    public PhenotypeInfo<string> StringMatchableResolution { get; }

    /// <summary>
    /// Likelihood of this genotype.
    ///
    /// Stored with the genotype to avoid dictionary lookups when calculating final likelihoods, as looking up the same genotype multiple times
    /// for different patient/donor pairs is inefficient 
    /// </summary>
    public decimal GenotypeLikelihood { get; }

    public GenotypeAtDesiredResolutions(
        PhenotypeInfo<HlaAtKnownTypingCategory> haplotypeResolution,
        PhenotypeInfo<string> stringMatchableResolution,
        decimal genotypeLikelihood)
    {
        HaplotypeResolution = haplotypeResolution.ToHlaNames();
        StringMatchableResolution = stringMatchableResolution;
        GenotypeLikelihood = genotypeLikelihood;
    }
}