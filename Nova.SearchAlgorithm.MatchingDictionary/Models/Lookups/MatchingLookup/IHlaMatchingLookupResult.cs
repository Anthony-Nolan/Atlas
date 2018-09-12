using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup
{
    /// <summary>
    /// Lookup result with data required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingLookupResult : IHlaLookupResult, IMatchingPGroups
    {
        bool IsNullExpressingTyping { get; }
    }
}
