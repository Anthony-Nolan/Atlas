using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
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
