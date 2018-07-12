using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class MatchResultWithPreCalculatedHlaMatchInfo
    {
        public MatchResult MatchResult { get; set; }
        public PhenotypeInfo<IEnumerable<PreCalculatedHlaMatchInfo>> PreCalculatedHlaMatchInfo { get; set; }
    }
}