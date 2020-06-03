using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IMatchedOn
    {
        HlaTyping HlaTyping { get; }
        HlaTyping TypingUsedInMatching { get; }
    }
}
