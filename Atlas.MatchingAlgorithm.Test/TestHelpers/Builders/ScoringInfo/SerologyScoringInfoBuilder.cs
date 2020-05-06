using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo
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