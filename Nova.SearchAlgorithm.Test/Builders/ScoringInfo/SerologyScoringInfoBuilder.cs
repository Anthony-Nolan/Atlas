using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class SerologyScoringInfoBuilder
    {
        private SerologyScoringInfo scoringInfo;

        public SerologyScoringInfoBuilder()
        {
            scoringInfo = new SerologyScoringInfo(
                SerologySubtype.Broad,
                new List<SerologyEntry>()
                );
        }

        public SerologyScoringInfoBuilder WithSerologySubtype(SerologySubtype serologySubtype)
        {
            scoringInfo = new SerologyScoringInfo(serologySubtype, scoringInfo.MatchingSerologies);
            return this;
        }
        
        public SerologyScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> serologyEntries)
        {
            scoringInfo = new SerologyScoringInfo(scoringInfo.SerologySubtype, serologyEntries);
            return this;
        }

        public SerologyScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}