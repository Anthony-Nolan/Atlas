using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>One difference between the set computed live and the stored set.</summary>
internal class ParityDifference
{
    /// <summary>
    /// For the whole set: <c>HaplotypeFrequencySet</c>, <c>IsUnrepresented</c>, <c>GenotypeCount</c> or
    /// <c>SumOfLikelihoods</c>. For one genotype: <c>GenotypeOnlyInLive</c>, <c>GenotypeOnlyInStored</c> or
    /// <c>GenotypeLikelihood</c>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public GenotypeSetDifferenceKind Kind { get; set; }

    /// <summary>For one genotype: the genotype as P groups.</summary>
    public string StringMatchableResolution { get; set; }

    /// <summary>For one genotype: the genotype at the resolution of the haplotype frequency set.</summary>
    public string HaplotypeResolution { get; set; }

    /// <summary>
    /// The value in the live set, as text with all its digits, so that a client that reads numbers as doubles does not
    /// round it. Not given when the genotype is in the stored set only.
    /// </summary>
    public string Live { get; set; }

    /// <summary>The value in the stored set, as text with all its digits. Not given when the genotype is in the live set only.</summary>
    public string Stored { get; set; }

    public static ParityDifference From(GenotypeSetDifference difference) => new()
    {
        Kind = difference.Kind,
        StringMatchableResolution = difference.StringMatchableResolution?.PrettyPrint(),
        HaplotypeResolution = difference.HaplotypeResolution?.PrettyPrint(),
        Live = difference.Live,
        Stored = difference.Stored
    };
}
