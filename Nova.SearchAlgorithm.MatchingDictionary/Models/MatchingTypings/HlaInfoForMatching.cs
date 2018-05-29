using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class HlaInfoForMatching: IHlaInfoForAlleleMatcher
    {
        public List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        public List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        public List<RelDnaSer> DnaToSerologyRelationships { get; }

        public HlaInfoForMatching(
            List<IAlleleInfoForMatching> alleleInfoForMatching, 
            List<ISerologyInfoForMatching> serologyInfoForMatching, 
            List<RelDnaSer> dnaToSerologyRelationships)
        {
            AlleleInfoForMatching = alleleInfoForMatching;
            SerologyInfoForMatching = serologyInfoForMatching;
            DnaToSerologyRelationships = dnaToSerologyRelationships;
        }
    }
}
