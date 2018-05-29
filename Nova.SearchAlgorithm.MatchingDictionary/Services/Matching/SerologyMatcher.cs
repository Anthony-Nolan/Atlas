using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Creates a complete collection of matched serologies
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class SerologyMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            var matchedHlaQuery =
                from serologyInfo in hlaInfo.SerologyInfoForMatching
                let allelesInfo = GetAlleleMappingsForSerology(hlaInfo, serologyInfo).ToArray()
                let pGroups = allelesInfo.SelectMany(allele => allele.MatchingPGroups).Distinct()
                let gGroups = allelesInfo.SelectMany(allele => allele.MatchingGGroups).Distinct()
                select new MatchedSerology(serologyInfo, pGroups, gGroups);

            return matchedHlaQuery.ToArray();
        }

        private static IEnumerable<IAlleleInfoForMatching> GetAlleleMappingsForSerology(IHlaInfoToMapAlleleToSerology hlaInfo, ISerologyInfoForMatching serologyInfo)
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
