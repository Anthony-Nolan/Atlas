using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedSerology : IMatchedHla, IMatchingDictionarySource<SerologyTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyTyping> MatchingSerologies { get; }
        public SerologyTyping TypingForMatchingDictionary => (SerologyTyping) HlaTyping;

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
