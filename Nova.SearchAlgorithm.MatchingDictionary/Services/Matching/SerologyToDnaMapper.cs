using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    internal class SerologyToDnaMapper
    {
        public IEnumerable<IAlleleInfoForMatching> GetAlleleMappingsForSerology(IHlaInfoToMapSerologyToDna hlaInfo, ISerologyInfoForMatching serologyInfo)
        {
            var matchLocus = serologyInfo.HlaTyping.MatchLocus;
            var matchingSerologies = serologyInfo.MatchingSerologies.Select(m => m.Name);

            return
                from alleleInfo in hlaInfo.AlleleInfoForMatching
                join dnaToSer in hlaInfo.DnaToSerologyRelationships
                    on new { alleleInfo.TypingUsedInMatching.WmdaLocus, alleleInfo.TypingUsedInMatching.Name }
                    equals new { dnaToSer.WmdaLocus, dnaToSer.Name }
                where alleleInfo.TypingUsedInMatching.MatchLocus.Equals(matchLocus)
                      && dnaToSer.Serologies.Intersect(matchingSerologies).Any()
                select alleleInfo;
        }
    }
}
