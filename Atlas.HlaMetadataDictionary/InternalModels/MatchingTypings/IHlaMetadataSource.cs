using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    /// <summary>
    /// Identifies classes that can serve as a data source
    /// for the creation of a HLA Metadata.
    /// </summary>
    internal interface IHlaMetadataSource<out THlaTyping> where THlaTyping : HlaTyping
    {
        IEnumerable<string> MatchingPGroups { get; }
        IEnumerable<string> MatchingGGroups { get; }
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
        THlaTyping TypingForHlaMetadata { get; }
    }
}
