using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class HlaInfoForMatching: IHlaInfoToMapAlleleToSerology, IHlaInfoToMapSerologyToAllele
    {
        public List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        public List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        public List<RelDnaSer> AlleleToSerologyRelationships { get; }

        public HlaInfoForMatching(
            List<IAlleleInfoForMatching> alleleInfoForMatching, 
            List<ISerologyInfoForMatching> serologyInfoForMatching, 
            List<RelDnaSer> alleleToSerologyRelationships)
        {
            AlleleInfoForMatching = alleleInfoForMatching;
            SerologyInfoForMatching = serologyInfoForMatching;
            AlleleToSerologyRelationships = alleleToSerologyRelationships;
        }
    }
}
