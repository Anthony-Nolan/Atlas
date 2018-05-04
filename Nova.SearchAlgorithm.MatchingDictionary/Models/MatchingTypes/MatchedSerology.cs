using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class MatchedSerology : IMatchedHla
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<Serology> MatchingSerologies { get; }

        public MatchedSerology(ISerologyToSerology matchedSerology, IEnumerable<string> matchingPGroups)
        {
            HlaType = matchedSerology.HlaType;
            TypeUsedInMatching = matchedSerology.TypeUsedInMatching;
            MatchingPGroups = matchingPGroups;
            MatchingSerologies = matchedSerology.MatchingSerologies;
        }
    }
}
