using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    public class MatchedHlaSourceData
    {
        public List<IAlleleToPGroup> AlleleToPGroups { get; set; }
        public List<ISerologyToSerology> SerologyToSerology { get; set; }
        public List<RelDnaSer> RelDnaSer { get; set; }
    }
}
