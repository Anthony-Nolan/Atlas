using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups
{
    public class HlaLookupResultCollections
    {
        public IEnumerable<IHlaLookupResult> AlleleNameLookupResults { get; set; }
        public IEnumerable<IHlaLookupResult> HlaMatchingLookupResults { get; set; }
        public IEnumerable<IHlaLookupResult> HlaScoringLookupResults { get; set; }
        public IEnumerable<IHlaLookupResult> Dpb1TceGroupLookupResults { get; set; }
    }
}
