using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class MatchedSerology : IMatchedHla, IHlaMetadataSource<SerologyTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public SerologyTyping TypingForHlaMetadata => (SerologyTyping) HlaTyping;

        public MatchedSerology(ISerologyInfoForMatching matchedSerology, IEnumerable<string> matchingPGroups, IEnumerable<string> matchingGGroups)
        {
            HlaTyping = matchedSerology.HlaTyping;
            TypingUsedInMatching = matchedSerology.TypingUsedInMatching;
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchedSerology.MatchingSerologies;
        }     
    }
}
