using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    internal class SerologyToAlleleMapper
    {
        public IEnumerable<IAlleleInfoForMatching> GetAlleleMappingsForSerology(IHlaInfoToMapSerologyToAllele hlaInfo, ISerologyInfoForMatching serologyInfo)
        {
            var locus = serologyInfo.HlaTyping.Locus;
            var matchingSerologyNames = serologyInfo.MatchingSerologies.Select(m => m.SerologyTyping.Name);

            return
                from alleleInfo in hlaInfo.AlleleInfoForMatching
                join alleleToSerology in hlaInfo.AlleleToSerologyRelationships
                    on new { WmdaLocus = alleleInfo.TypingUsedInMatching.TypingLocus, alleleInfo.TypingUsedInMatching.Name }
                    equals new { WmdaLocus = alleleToSerology.TypingLocus, alleleToSerology.Name }
                where alleleInfo.TypingUsedInMatching.Locus.Equals(locus)
                      && alleleToSerology.Serologies.Intersect(matchingSerologyNames).Any()
                select alleleInfo;
        }
    }
}
