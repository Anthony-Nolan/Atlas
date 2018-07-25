using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    internal class SerologyToAlleleMapper
    {
        public IEnumerable<IAlleleInfoForMatching> GetAlleleMappingsForSerology(IHlaInfoToMapSerologyToAllele hlaInfo, ISerologyInfoForMatching serologyInfo)
        {
            var matchLocus = serologyInfo.HlaTyping.MatchLocus;
            var matchingSerologyNames = serologyInfo.MatchingSerologies.Select(m => m.SerologyTyping.Name);

            return
                from alleleInfo in hlaInfo.AlleleInfoForMatching
                join alleleToSerology in hlaInfo.AlleleToSerologyRelationships
                    on new { WmdaLocus = alleleInfo.TypingUsedInMatching.Locus, alleleInfo.TypingUsedInMatching.Name }
                    equals new { WmdaLocus = alleleToSerology.Locus, alleleToSerology.Name }
                where alleleInfo.TypingUsedInMatching.MatchLocus.Equals(matchLocus)
                      && alleleToSerology.Serologies.Intersect(matchingSerologyNames).Any()
                select alleleInfo;
        }
    }
}
