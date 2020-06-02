using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Metadata required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringMetadata : ISerialisableHlaMetadata
    {
        IHlaScoringInfo HlaScoringInfo { get; }
        IEnumerable<IHlaScoringMetadata> GetInTermsOfSingleAlleleScoringMetadata();
    }
}
