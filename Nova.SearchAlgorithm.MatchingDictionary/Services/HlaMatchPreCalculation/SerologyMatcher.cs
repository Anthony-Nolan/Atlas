using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Creates a complete collection of matched serologies
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class SerologyMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            var serologyToAlleleMapper = new SerologyToAlleleMapper();

            var matchedHlaQuery = hlaInfo.SerologyInfoForMatching
                .AsParallel()
                .Select(serologyInfo => new {serologyInfo, allelesInfo = serologyToAlleleMapper.GetAlleleMappingsForSerology(hlaInfo, serologyInfo)})
                .Select(t => new {t, pGroups = t.allelesInfo.SelectMany(allele => allele.MatchingPGroups).Distinct()})
                .Select(t => new {t, gGroups = t.t.allelesInfo.SelectMany(allele => allele.MatchingGGroups).Distinct()})
                .Select(t => new MatchedSerology(t.t.t.serologyInfo, t.t.pGroups, t.gGroups))
                .AsEnumerable();

            return matchedHlaQuery;
        }
    }
}