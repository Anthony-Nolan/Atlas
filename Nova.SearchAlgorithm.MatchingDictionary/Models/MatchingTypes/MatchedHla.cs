using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchedHla : IMatchingPGroups, IMatchingSerology
    {
        
    }

    public class MatchedHla : IMatchedHla
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<Serology> MatchingSerologies { get; }

        public MatchedHla(
            HlaType hlaType, 
            HlaType typeUsedInMatching, 
            IEnumerable<string> matchingPGroups, 
            IEnumerable<Serology> matchingSerologies)
        {
            HlaType = hlaType;
            TypeUsedInMatching = typeUsedInMatching;
            MatchingPGroups = matchingPGroups;
            MatchingSerologies = matchingSerologies;
        }

        public override string ToString()
        {
            return $"{HlaType} ({TypeUsedInMatching}) " +
                   $"{{ matchingPGroups: {string.Join("/", MatchingPGroups)} }}, " +
                   $"{{ matchingSerology: {string.Join("/", MatchingSerologies.Select(m => m))} }}";
        }
    }
}
