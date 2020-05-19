using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
{
    /// <summary>
    /// Identifies classes that can serve as a data source
    /// for the creation of a HLA lookup result.
    /// </summary>
    internal interface IHlaLookupResultSource<out THlaTyping> : 
        IMatchingPGroups, IMatchingGGroups, IMatchingSerologies 
        where THlaTyping : HlaTyping
    {
        THlaTyping TypingForHlaLookupResult { get; }
    }
}
