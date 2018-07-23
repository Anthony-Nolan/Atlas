using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedAllele : IMatchedHla, IHlaLookupResultSource<AlleleTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyTyping> MatchingSerologies { get; }
        public IEnumerable<SerologyMappingForAllele> AlleleToSerologyMappings { get; }
        public AlleleTyping TypingForHlaLookupResult => (AlleleTyping) HlaTyping;

        public MatchedAllele(IAlleleInfoForMatching matchedAllele, IEnumerable<SerologyMappingForAllele> alleleToSerologyMappings)
        {
            HlaTyping = matchedAllele.HlaTyping;
            TypingUsedInMatching = matchedAllele.TypingUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingGGroups = matchedAllele.MatchingGGroups;

            var serologyMappingsList = alleleToSerologyMappings.ToList();
            MatchingSerologies = serologyMappingsList.SelectMany(m => m.AllMatchingSerology.Select(s => s.SerologyTyping));
            AlleleToSerologyMappings = serologyMappingsList;
        }
    }
}
