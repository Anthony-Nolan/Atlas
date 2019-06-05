using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class SerologyScoringInfoBuilder
    {
        private SerologyScoringInfo scoringInfo;

        public SerologyScoringInfoBuilder()
        {
            scoringInfo = new SerologyScoringInfo(
                new List<SerologyEntry>()
                );
        }
        
        public SerologyScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> serologyEntries)
        {
            scoringInfo = new SerologyScoringInfo(serologyEntries);

            return this;
        }

        public SerologyScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}