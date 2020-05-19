using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
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