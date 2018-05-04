using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class MatchedAllele : IMatchedHla
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<Serology> MatchingSerologies { get; }
        public IEnumerable<SerologyMappingInfo> SerologyMappings { get; }

        public MatchedAllele(IMatchingPGroups matchedAllele, IList<SerologyMappingInfo> serologyMappings)
        {
            HlaType = matchedAllele.HlaType;
            TypeUsedInMatching = matchedAllele.TypeUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingSerologies = serologyMappings.SelectMany(m => m.AllMatchingSerology.Select(s => s.Serology));
            SerologyMappings = serologyMappings;
        }
    }
}
