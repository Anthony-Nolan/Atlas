using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    internal static class SerologyToAlleleMapper
    {
        /// <returns>Alleles that map to one or more serologies that match the submitted serology.</returns>
        public static IEnumerable<SerologyToAlleleMapping> GetAlleleMappingsForSerology(SerologyInfoForMatching serologyInfo, IHlaInfoToMapSerologyToAllele hlaInfo)
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
                select new SerologyToAlleleMapping
                {
                    MatchedAllele = alleleInfo,
                    SerologyBridge = alleleToSerology.Serologies.Intersect(matchingSerologyNames)
                };
        }
    }
}
