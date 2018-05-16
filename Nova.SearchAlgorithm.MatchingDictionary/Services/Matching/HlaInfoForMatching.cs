using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    public class HlaInfoForMatching
    {
        public List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        public List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        public List<RelDnaSer> RelDnaSer { get; }

        public HlaInfoForMatching(
            List<IAlleleInfoForMatching> alleleInfoForMatching, 
            List<ISerologyInfoForMatching> serologyInfoForMatching, 
            List<RelDnaSer> relDnaSer)
        {
            AlleleInfoForMatching = alleleInfoForMatching;
            SerologyInfoForMatching = serologyInfoForMatching;
            RelDnaSer = relDnaSer;
        }
    }
}
