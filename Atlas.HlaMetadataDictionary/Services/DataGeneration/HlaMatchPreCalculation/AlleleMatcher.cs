using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
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
                .Select(alleleInfo => new MatchedAllele(
                    alleleInfo, 
                    alleleToSerologyMapper.GetSerologiesMatchingToAllele(hlaInfo, (AlleleTyping)alleleInfo.TypingUsedInMatching))
                )
                .AsEnumerable();

            return matchedHlaQuery;
        }
    }
}