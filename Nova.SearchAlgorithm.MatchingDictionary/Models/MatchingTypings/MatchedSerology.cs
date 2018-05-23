using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedSerology : IMatchedHla, IMatchingDictionarySource<SerologyTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<SerologyTyping> MatchingSerologies { get; }
        public SerologyTyping TypingForMatchingDictionary => (SerologyTyping) HlaTyping;

        public MatchedSerology(ISerologyInfoForMatching matchedSerology, IEnumerable<string> matchingPGroups)
        {
            HlaTyping = matchedSerology.HlaTyping;
            TypingUsedInMatching = matchedSerology.TypingUsedInMatching;
            MatchingPGroups = matchingPGroups;
            MatchingSerologies = matchedSerology.MatchingSerologies;
        }     
    }
}
