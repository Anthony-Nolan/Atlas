using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }
        HlaTyping TypingUsedInMatching { get; }
    }
}
