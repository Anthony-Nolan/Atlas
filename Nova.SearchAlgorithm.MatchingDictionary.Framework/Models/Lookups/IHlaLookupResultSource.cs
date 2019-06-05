using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
{
    /// <summary>
    /// Identifies classes that can serve as a data source
    /// for the creation of a HLA lookup result.
    /// </summary>
    public interface IHlaLookupResultSource<out THlaTyping> : 
        IMatchingPGroups, IMatchingGGroups, IMatchingSerologies 
        where THlaTyping : HlaTyping
    {
        THlaTyping TypingForHlaLookupResult { get; }
    }
}
