using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }
        HlaTyping TypingUsedInMatching { get; }
    }
}
