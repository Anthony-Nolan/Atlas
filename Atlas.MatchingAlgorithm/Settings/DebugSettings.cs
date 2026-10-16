namespace Atlas.MatchingAlgorithm.Settings;

/// <summary>
/// Settings of the debug endpoints of the matching algorithm functions app. Read from <c>Debug</c>.
/// </summary>
public class DebugSettings
{
    /// <summary>
    /// When true, the genotype set compute endpoint can save what it computes to the active matching database, where
    /// search reads it. Set it to true in test environments only. When false, a request to save is refused and changes
    /// nothing.
    /// </summary>
    public bool AllowPrecomputeWrites { get; set; }
}
