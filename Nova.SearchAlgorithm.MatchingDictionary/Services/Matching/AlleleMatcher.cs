using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Creates a complete collection of matched alleles
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class AlleleMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            var dnaToSerologyMapper = new DnaToSerologyMapper();

            var matchedHlaQuery =
                from alleleInfo in hlaInfo.AlleleInfoForMatching
                let dnaToSerologyMapping = dnaToSerologyMapper.GetSerologyMappingsForAllele(
                    hlaInfo, (AlleleTyping)alleleInfo.TypingUsedInMatching)
                select new MatchedAllele(alleleInfo, dnaToSerologyMapping);

            return matchedHlaQuery.ToArray();
        }        
    }
}
