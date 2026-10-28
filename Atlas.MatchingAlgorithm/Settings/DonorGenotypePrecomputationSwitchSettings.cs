namespace Atlas.MatchingAlgorithm.Settings;

/// <summary>
/// Turns the precomputation of donor genotype sets on or off for an installation: stage 65 of the data refresh, and the
/// precomputation after a differential donor import. Read from <c>DonorGenotypePrecomputation</c> by the data refresh and
/// donor management function apps.
/// </summary>
/// <remarks>
/// An installation that does not use match prediction does not need the genotype sets. Turn it off there. Search then
/// imputes every donor live, so also keep <c>MatchPrediction:Precompute:Mode</c> at <c>ForceLive</c>.
/// </remarks>
public class DonorGenotypePrecomputationSwitchSettings
{
    /// <summary>
    /// When false, the data refresh skips its donor genotype precomputation stage and marks it as complete, and a donor
    /// import does not precompute the donors that it writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True by default, so that an installation that has not had the app setting deployed yet behaves as before.
    /// </para>
    ///
    /// <para>
    /// A change does not delete the stored genotype sets. They are deleted as before, whatever the value: all of them when a
    /// data refresh removes the donor data of its database, and those of a donor when a donor import updates it.
    /// </para>
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
