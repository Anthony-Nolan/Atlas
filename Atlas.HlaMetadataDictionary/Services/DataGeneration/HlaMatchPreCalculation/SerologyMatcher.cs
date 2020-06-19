using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    /// <summary>
    /// Creates a complete collection of matched serologies
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class SerologyMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            var matchedHlaQuery = hlaInfo.SerologyInfoForMatching
                .AsParallel()
                .Select(serologyInfo =>
                {
                    var allelesInfo = SerologyToAlleleMapper.GetAlleleMappingsForSerology(serologyInfo, hlaInfo).ToList();
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