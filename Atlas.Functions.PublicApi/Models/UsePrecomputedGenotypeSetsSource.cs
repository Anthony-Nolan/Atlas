namespace Atlas.Functions.PublicApi.Models;

/// <summary>What chose a search's <c>UsePrecomputedGenotypeSets</c> value, when the Atlas Public API accepted it.</summary>
internal enum UsePrecomputedGenotypeSetsSource
{
    /// <summary>The request named the value. It is kept, and the switch and the share are ignored.</summary>
    Request,

    /// <summary>The request left the value empty, so the switch and the share chose it.</summary>
    FeatureFlag
}
