using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringMetadata : ISerialisableHlaMetadata
    {
        IHlaScoringInfo HlaScoringInfo { get; }
        IEnumerable<IHlaScoringMetadata> GetInTermsOfSingleAlleleScoringMetadata();
    }
}
