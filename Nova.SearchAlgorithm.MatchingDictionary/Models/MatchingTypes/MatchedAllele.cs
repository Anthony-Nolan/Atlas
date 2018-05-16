using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class MatchedAllele : IMatchedHla, IDictionarySource<Allele>
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<Serology> MatchingSerologies { get; }
        public IEnumerable<RelDnaSerMapping> RelDnaSerMappings { get; }
        public Allele TypeForDictionary => (Allele) HlaType;

        public MatchedAllele(IAlleleInfoForMatching matchedAllele, IList<RelDnaSerMapping> relDnaSerMappings)
        {
            HlaType = matchedAllele.HlaType;
            TypeUsedInMatching = matchedAllele.TypeUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingSerologies = relDnaSerMappings.SelectMany(m => m.AllMatchingSerology.Select(s => s.Serology));
            RelDnaSerMappings = relDnaSerMappings;
        }
    }
}
