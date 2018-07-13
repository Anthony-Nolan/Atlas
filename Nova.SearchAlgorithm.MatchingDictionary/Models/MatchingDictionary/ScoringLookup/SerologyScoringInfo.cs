using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    public class SerologyScoringInfo : IPreCalculatedScoringInfo
    {
        public SerologySubtype SerologySubtype { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public SerologyScoringInfo(
            SerologySubtype serologySubtype, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            SerologySubtype = serologySubtype;
            MatchingSerologies = matchingSerologies;
        }
    }
}
