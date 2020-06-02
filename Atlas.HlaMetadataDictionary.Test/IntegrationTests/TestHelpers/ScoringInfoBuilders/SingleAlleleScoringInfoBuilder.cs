using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.ScoringInfoBuilders
{
    public class SingleAlleleScoringInfoBuilder
    {
        private SingleAlleleScoringInfo scoringInfo;

        public SingleAlleleScoringInfoBuilder()
        {
            scoringInfo = new SingleAlleleScoringInfo(
                "allele-name",
                AlleleTypingStatus.GetDefaultStatus(),
                "p-group",
                "g-group");
        }

        public SingleAlleleScoringInfoBuilder WithAlleleName(string alleleName)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                alleleName, 
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                scoringInfo.MatchingGGroup);

            return this;
        }
        
        public SingleAlleleScoringInfoBuilder WithAlleleTypingStatus(AlleleTypingStatus alleleTypingStatus)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName, 
                alleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                scoringInfo.MatchingGGroup);

            return this;
        }
        
        public SingleAlleleScoringInfoBuilder WithMatchingPGroup(string pGroup)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName, 
                scoringInfo.AlleleTypingStatus, 
                pGroup, 
                scoringInfo.MatchingGGroup);

            return this;
        }
        
        public SingleAlleleScoringInfoBuilder WithMatchingGGroup(string gGroup)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName, 
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                gGroup);

            return this;
        }

        public SingleAlleleScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> serologyEntries)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName, 
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                scoringInfo.MatchingGGroup,
                serologyEntries);

            return this;
        }

        public SingleAlleleScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}