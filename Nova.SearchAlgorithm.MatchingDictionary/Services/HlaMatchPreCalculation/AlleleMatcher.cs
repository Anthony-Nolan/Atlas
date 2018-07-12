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

            var matchedHlaQuery =
                from alleleInfo in hlaInfo.AlleleInfoForMatching
                let serologyMappings = alleleToSerologyMapper.GetSerologyMappingsForAllele(
                    hlaInfo, (AlleleTyping)alleleInfo.TypingUsedInMatching)
                select new MatchedAllele(alleleInfo, serologyMappings);

            return matchedHlaQuery;
        }        
    }
}
