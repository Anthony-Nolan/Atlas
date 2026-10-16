#nullable enable

using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>One difference between a genotype set computed live and its stored set.</summary>
/// <param name="StringMatchableResolution">
/// For a difference of one genotype: the genotype as P groups. Null for a difference of the whole set.
/// </param>
/// <param name="HaplotypeResolution">
/// For a difference of one genotype: the genotype at the resolution of the frequency set. Null for a difference of the
/// whole set.
/// </param>
/// <param name="Live">
/// The value in the live set, as text with all its digits: a client that reads a JSON number as a double could round two
/// different likelihoods to the same number. Null when the genotype is in the stored set only.
/// </param>
/// <param name="Stored">The value in the stored set, as text with all its digits. Null when the genotype is in the live set only.</param>
public sealed record GenotypeSetDifference(
    GenotypeSetDifferenceKind Kind,
    PhenotypeInfo<string>? StringMatchableResolution,
    PhenotypeInfo<string>? HaplotypeResolution,
    string? Live,
    string? Stored);

public enum GenotypeSetDifferenceKind
{
    /// <summary>
    /// For a donor: the stored row has another frequency set than the live set, because the row is out of date or the
    /// request names another set. The other differences then come from the two sets, not from the stored data.
    /// </summary>
    HaplotypeFrequencySet,

    /// <summary>One set is unrepresented, and the other is not.</summary>
    IsUnrepresented,

    /// <summary>The sets have different numbers of genotypes.</summary>
    GenotypeCount,

    /// <summary>The sums of likelihoods are different.</summary>
    SumOfLikelihoods,

    /// <summary>A genotype is in the live set only, or it is in the live set more times than in the stored set.</summary>
    GenotypeOnlyInLive,

    /// <summary>A genotype is in the stored set only, or it is in the stored set more times than in the live set.</summary>
    GenotypeOnlyInStored,

    /// <summary>A genotype is in both sets, with a different likelihood.</summary>
    GenotypeLikelihood
}
