using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.ScoringInfoBuilders
{
    public class ConsolidatedMolecularScoringInfoBuilder
    {
        private ConsolidatedMolecularScoringInfo scoringInfo;

        public ConsolidatedMolecularScoringInfoBuilder()
        {
            scoringInfo = new ConsolidatedMolecularScoringInfo(
                new List<string>(), 
                new List<string>(), 
                new List<SerologyEntry>()
                );
        }
        
        public ConsolidatedMolecularScoringInfoBuilder WithMatchingPGroups(IEnumerable<string> pGroups)
        {
            scoringInfo = new ConsolidatedMolecularScoringInfo(pGroups, scoringInfo.MatchingGGroups, scoringInfo.MatchingSerologies);
            return this;
        }
        
        public ConsolidatedMolecularScoringInfoBuilder WithMatchingGGroups(IEnumerable<string> gGroups)
        {
            scoringInfo = new ConsolidatedMolecularScoringInfo(scoringInfo.MatchingPGroups, gGroups, scoringInfo.MatchingSerologies);
            return this;
        }
        
        public ConsolidatedMolecularScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> matchingSerologies)
        {
            scoringInfo = new ConsolidatedMolecularScoringInfo(scoringInfo.MatchingPGroups, scoringInfo.MatchingGGroups, matchingSerologies);
            return this;
        }

        public ConsolidatedMolecularScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}