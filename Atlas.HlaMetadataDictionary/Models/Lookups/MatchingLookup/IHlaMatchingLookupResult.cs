using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup
{
    /// <summary>
    /// Lookup result with data required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingLookupResult : ISerialisableHlaMetadata, IMatchingPGroups
    {
        bool IsNullExpressingTyping { get; }
    }
}
