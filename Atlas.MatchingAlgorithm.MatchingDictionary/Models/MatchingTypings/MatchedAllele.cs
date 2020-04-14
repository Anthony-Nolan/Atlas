using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedAllele : IMatchedHla, IHlaLookupResultSource<AlleleTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public AlleleTyping TypingForHlaLookupResult => (AlleleTyping) HlaTyping;

        public MatchedAllele(IAlleleInfoForMatching matchedAllele, IEnumerable<MatchingSerology> matchingSerologies)
        {
            HlaTyping = matchedAllele.HlaTyping;
            TypingUsedInMatching = matchedAllele.TypingUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingGGroups = matchedAllele.MatchingGGroups;
            MatchingSerologies = matchingSerologies;
        }
    }
}
