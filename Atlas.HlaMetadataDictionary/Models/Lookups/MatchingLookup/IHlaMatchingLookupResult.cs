using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup
{
    /// <summary>
    /// Lookup result with data required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingLookupResult : IHlaLookupResult, IMatchingPGroups
    {
        bool IsNullExpressingTyping { get; }
    }
}
