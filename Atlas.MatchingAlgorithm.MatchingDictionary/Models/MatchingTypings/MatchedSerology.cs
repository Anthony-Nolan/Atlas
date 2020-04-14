using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedSerology : IMatchedHla, IHlaLookupResultSource<SerologyTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public SerologyTyping TypingForHlaLookupResult => (SerologyTyping) HlaTyping;

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
