namespace Atlas.MatchPrediction.ExternalInterface.Settings;

/// <summary>
/// The kill-switch for reading precomputed donor genotype sets at search time (ATL-221).
/// </summary>
/// <remarks>
/// Match prediction probabilities are clinically sensitive, so this gates the whole precomputed path: the lookup, the
/// decode, and the write-back of donors computed live. When it is off for a search, every donor runs the complete live
/// pipeline.
/// </remarks>
public class PrecomputedGenotypeSetSettings
{
    /// <summary>
    /// Defaults to <see cref="PrecomputedGenotypeSetMode.ForceLive"/>, so a host that has not had the setting
    /// configured computes every donor live, whatever a search request asks for.
    /// </summary>
    public PrecomputedGenotypeSetMode Mode { get; set; } = PrecomputedGenotypeSetMode.ForceLive;
}

/// <summary>
/// How the kill-switch combines with a search's <c>SearchRequest.UsePrecomputedGenotypeSets</c> override.
/// </summary>
public enum PrecomputedGenotypeSetMode
{
    /// <summary>
    /// The hard off: every search is computed live, and the request's override is ignored. Use it until clinical
    /// sign-off, and to stop the precomputed path at once, without a deploy, if a defect is found in it.
    /// </summary>
    ForceLive,

    /// <summary>Live by default. A search can opt in with <c>UsePrecomputedGenotypeSets = true</c>.</summary>
    DefaultLive,

    /// <summary>Precomputed by default. A search can opt out with <c>UsePrecomputedGenotypeSets = false</c>.</summary>
    DefaultPrecomputed
}
