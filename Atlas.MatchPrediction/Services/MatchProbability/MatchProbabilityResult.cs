using System;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.MatchPrediction.ApplicationInsights;
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
/// <param name="Timing">How long the donor's genotype set and whole match took; summed per batch (ATL-474).</param>
public record MatchProbabilityResult(
    MatchProbabilityResponse Response,
    int DonorGenotypeCount,
    DonorGenotypeSetSource GenotypeSetSource = DonorGenotypeSetSource.PrecomputeDisabled,
    DonorGenotypeSetToStore GenotypeSetToStore = null,
    DonorGenotypeSetTiming Timing = null);

/// <summary>
/// Where one donor input's match prediction time went, as far as the genotype set is concerned (ATL-474).
/// </summary>
/// <param name="LookupTime">
/// Finding the donor's stored row in the batch's rows, and decoding it when it is used. Zero when the batch has no
/// context. The SQL read is per batch, so it is not part of this (see <see cref="DonorGenotypeSetBatchContext.ReadTime"/>).
/// </param>
/// <param name="LiveComputeTime">The live imputation; null when the stored set was used.</param>
/// <param name="StoredBytes">The size of the stored payload that was decoded; null when none was.</param>
/// <param name="EncodeTime">Encoding a live set to store it; zero when it is not stored.</param>
/// <param name="TotalTime">The whole match prediction for the donor input, from validation to the response.</param>
/// <param name="LiveImputationStages">Where <paramref name="LiveComputeTime"/> went, by stage; null when the donor was not imputed live.</param>
public sealed record DonorGenotypeSetTiming(
    TimeSpan LookupTime,
    TimeSpan? LiveComputeTime,
    int? StoredBytes,
    TimeSpan EncodeTime,
    TimeSpan TotalTime,
    LiveImputationStageTimings LiveImputationStages = null);
