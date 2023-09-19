using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class MatchedAllele : IMatchedHla, IHlaMetadataSource<AlleleTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public List<string> MatchingPGroups { get; }
        public List<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public AlleleTyping TypingForHlaMetadata => (AlleleTyping)HlaTyping;

        public MatchedAllele(AlleleInfoForMatching matchedAllele, IEnumerable<MatchingSerology> matchingSerologies)
        {
            HlaTyping = matchedAllele.HlaTyping;
            TypingUsedInMatching = matchedAllele.TypingUsedInMatching;
            MatchingPGroups = WrapAlleleGroupInList(matchedAllele.MatchingPGroup);
            MatchingGGroups = WrapAlleleGroupInList(matchedAllele.MatchingGGroup);
            MatchingSerologies = matchingSerologies;
        }

        private static List<string> WrapAlleleGroupInList(string alleleGroup)
        {
            return string.IsNullOrEmpty(alleleGroup) ? new List<string>() : new List<string> { alleleGroup };
        }
    }
}