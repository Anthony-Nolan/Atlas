using Atlas.Client.Models.Search.Results.MatchPrediction;

namespace Atlas.MatchPrediction.Services.MatchProbability;

/// <summary>
/// Wraps the public <see cref="MatchProbabilityResponse"/> with the internal, post-truncation donor imputed genotype
/// count that <see cref="GenotypeMatcher"/> computes en route. The count is deliberately kept off the public
/// (versioned) <see cref="MatchProbabilityResponse"/> model and surfaced here instead, so the parallel-batch worker can
/// record it against the batch row (see ATL-252) without altering the client contract.
/// </summary>
public record MatchProbabilityResult(MatchProbabilityResponse Response, int DonorGenotypeCount);
