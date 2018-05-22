using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    /// <summary>
    /// Identifies classes that can serve as a data source
    /// for the creation of a matching dictionary entry.
    /// </summary>
    public interface IDictionarySource<out THlaTyping> : IMatchingPGroups, IMatchingSerologies where THlaTyping : HlaTyping
    {
        THlaTyping TypingForDictionary { get; }
    }
}
