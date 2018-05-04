using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class MatchedSerology : MatchedHla
    {
        public MatchedSerology(IMatchingSerology matchedSerology, IEnumerable<string> matchingPGroups)
            : base(
                  matchedSerology.HlaType,
                  matchedSerology.TypeUsedInMatching,
                  matchingPGroups,
                  matchedSerology.MatchingSerologies)                  
        {
        }
    }
}
