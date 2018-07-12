using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Creates a complete collection of matched alleles
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class AlleleMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            var alleleToSerologyMapper = new AlleleToSerologyMapper();

            var matchedHlaQuery = hlaInfo.AlleleInfoForMatching
                .AsParallel()
                .Select(alleleInfo => new
                {
                    alleleInfo,
                    serologyMappings = alleleToSerologyMapper.GetSerologyMappingsForAllele(hlaInfo, (AlleleTyping) alleleInfo.TypingUsedInMatching)
                })
                .Select(t => new MatchedAllele(t.alleleInfo, t.serologyMappings))
                .AsEnumerable();

            return matchedHlaQuery;
        }
    }
}