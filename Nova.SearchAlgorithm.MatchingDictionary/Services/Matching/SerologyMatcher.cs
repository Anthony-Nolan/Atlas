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
            var serologyToAlleleMapper = new SerologyToAlleleMapper();

            var matchedHlaQuery =
                from serologyInfo in hlaInfo.SerologyInfoForMatching
                let allelesInfo = serologyToAlleleMapper.GetAlleleMappingsForSerology(hlaInfo, serologyInfo)
                let pGroups = allelesInfo.SelectMany(allele => allele.MatchingPGroups).Distinct()
                let gGroups = allelesInfo.SelectMany(allele => allele.MatchingGGroups).Distinct()
                select new MatchedSerology(serologyInfo, pGroups, gGroups);

            return matchedHlaQuery;
        }
    }
}
