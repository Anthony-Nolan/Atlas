using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup
{
    /// <summary>
    /// Lookup result with data required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingLookupResult : IHlaLookupResult, IMatchingPGroups
    {
        bool IsNullExpressingTyping { get; }
    }
}
