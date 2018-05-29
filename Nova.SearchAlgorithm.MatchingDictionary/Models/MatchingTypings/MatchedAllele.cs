using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedAllele : IMatchedHla, IMatchingDictionarySource<AlleleTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyTyping> MatchingSerologies { get; }
        public IEnumerable<RelDnaSerMapping> RelDnaSerMappings { get; }
        public AlleleTyping TypingForMatchingDictionary => (AlleleTyping) HlaTyping;

        public MatchedAllele(IAlleleInfoForMatching matchedAllele, IList<RelDnaSerMapping> relDnaSerMappings)
        {
            HlaTyping = matchedAllele.HlaTyping;
            TypingUsedInMatching = matchedAllele.TypingUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingGGroups = matchedAllele.MatchingGGroups;
            MatchingSerologies = relDnaSerMappings.SelectMany(m => m.AllMatchingSerology.Select(s => s.SerologyTyping));
            RelDnaSerMappings = relDnaSerMappings;
        }
    }
}
