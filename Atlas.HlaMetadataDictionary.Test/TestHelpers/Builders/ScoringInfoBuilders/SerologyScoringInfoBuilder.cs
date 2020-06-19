using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders
{
    public class SerologyScoringInfoBuilder
    {
        private SerologyScoringInfo scoringInfo;

        public SerologyScoringInfoBuilder()
        {
            scoringInfo = new SerologyScoringInfo(
                new List<SerologyEntry>(),
                new List<string>(),
                new List<string>()
                );
        }
        
        public SerologyScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> serologyEntries)
        {
            var newScoringInfo = new SerologyScoringInfo(serologyEntries, scoringInfo.MatchingGGroups, scoringInfo.MatchingPGroups);
            scoringInfo = newScoringInfo;

            return this;
        }

        public SerologyScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}