using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class MatchedAllele : MatchedHla
    {
        public IEnumerable<SerologyMappingInfo> SerologyMappings { get; }

        public MatchedAllele(IMatchingPGroups matchedAllele, IList<SerologyMappingInfo> serologyMappings)
            : base(
                  matchedAllele.HlaType,
                  matchedAllele.TypeUsedInMatching,
                  matchedAllele.MatchingPGroups,
                  serologyMappings.SelectMany(m => m.AllMatchingSerology.Select(s => s.Serology))
                  )
        {
            SerologyMappings = serologyMappings;
        }
    }
}
