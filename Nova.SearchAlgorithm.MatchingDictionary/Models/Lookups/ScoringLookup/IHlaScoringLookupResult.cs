using Nova.HLAService.Client.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringLookupResult : IHlaLookupResult
    {
        LookupResultCategory LookupResultCategory { get; }
        IHlaScoringInfo HlaScoringInfo { get; }

        /// <summary>
        /// Value should be set by lookup service before return.
        /// </summary>
        HlaTypingCategory HlaTypingCategory { get; set; }
    }
}
