using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class MatchResultWithHlaScoringLookupResults
    {
        public MatchResult MatchResult { get; set; }
        public PhenotypeInfo<IEnumerable<IHlaScoringLookupResult>> HlaScoringLookupResults { get; set; }
    }
}