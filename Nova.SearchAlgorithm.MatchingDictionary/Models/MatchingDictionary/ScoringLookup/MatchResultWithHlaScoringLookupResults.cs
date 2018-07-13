using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    public class MatchResultWithHlaScoringLookupResults
    {
        // TODO: This class to be dropped when scoring interfaces are updated
        public MatchResult MatchResult { get; set; }
        public PhenotypeInfo<IEnumerable<IHlaScoringLookupResult<IPreCalculatedScoringInfo>>> HlaScoringLookupResults { get; set; }
    }
}