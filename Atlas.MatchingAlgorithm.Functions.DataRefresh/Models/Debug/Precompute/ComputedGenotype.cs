using Atlas.MatchPrediction.Models;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>One genotype of a set.</summary>
internal class ComputedGenotype
{
    /// <summary>The genotype as P groups: the form that match counting compares.</summary>
    public string StringMatchableResolution { get; set; }

    /// <summary>The genotype at the resolution of the haplotype frequency set.</summary>
    public string HaplotypeResolution { get; set; }

    /// <summary>The exact likelihood, with no rounding.</summary>
    public decimal GenotypeLikelihood { get; set; }

    public static ComputedGenotype From(GenotypeAtDesiredResolutions genotype) => new()
    {
        StringMatchableResolution = genotype.StringMatchableResolution?.PrettyPrint(),
        HaplotypeResolution = genotype.HaplotypeResolution?.PrettyPrint(),
        GenotypeLikelihood = genotype.GenotypeLikelihood
    };
}
