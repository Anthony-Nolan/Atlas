using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

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
            var matchedHlaQuery = hlaInfo.AlleleInfoForMatching
                .AsParallel()
                .Select(alleleInfo => new MatchedAllele(
                    alleleInfo, 
                    AlleleToSerologyMapper.GetSerologiesMatchingToAllele((AlleleTyping)alleleInfo.TypingUsedInMatching, hlaInfo))
                )
                .AsEnumerable();

            return matchedHlaQuery;
        }
    }
}