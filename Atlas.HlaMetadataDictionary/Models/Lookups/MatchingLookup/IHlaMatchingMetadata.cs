using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup
{
    /// <summary>
    /// Metadata required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingMetadata : ISerialisableHlaMetadata, IMatchingPGroups
    {
        bool IsNullExpressingTyping { get; }
    }
}
