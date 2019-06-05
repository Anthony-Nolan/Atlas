using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }
        HlaTyping TypingUsedInMatching { get; }
    }
}
