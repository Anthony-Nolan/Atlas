using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
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
                .Select(serologyInfo =>
                {
                    var allelesInfo = serologyToAlleleMapper.GetAlleleMappingsForSerology(hlaInfo, serologyInfo).ToList();
                    return new MatchedSerology(
                        serologyInfo,
                        allelesInfo.SelectMany(a => a.MatchingPGroups).Distinct(),
                        allelesInfo.SelectMany(a => a.MatchingGGroups).Distinct()
                    );
                })
                .AsEnumerable();

            return matchedHlaQuery;
        }
    }
}