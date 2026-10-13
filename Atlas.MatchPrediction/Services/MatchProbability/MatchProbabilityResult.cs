using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchPrediction.Services.MatchProbability;

/// <summary>
/// Wraps the public <see cref="MatchProbabilityResponse"/> with the internal, post-truncation donor imputed genotype
/// count that <see cref="GenotypeMatcher"/> computes en route. The count is deliberately kept off the public
/// (versioned) <see cref="MatchProbabilityResponse"/> model and surfaced here instead, so the parallel-batch worker can
/// record it against the batch row (see ATL-252) without altering the client contract.
/// </summary>
/// <param name="GenotypeSetSource">
/// Whether the donor's genotype set was precomputed, or why it was computed live (ATL-221). Counted per batch.
/// </param>
/// <param name="GenotypeSetToStore">
/// A live-computed donor set the batch should store for reuse, or null. Already encoded, so the set itself is not held.
/// </param>
public record MatchProbabilityResult(
    MatchProbabilityResponse Response,
    int DonorGenotypeCount,
    DonorGenotypeSetSource GenotypeSetSource = DonorGenotypeSetSource.PrecomputeDisabled,
    DonorGenotypeSetToStore GenotypeSetToStore = null);
