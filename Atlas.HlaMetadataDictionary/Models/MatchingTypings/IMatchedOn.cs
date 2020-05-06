using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    public interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }
        HlaTyping TypingUsedInMatching { get; }
    }
}
