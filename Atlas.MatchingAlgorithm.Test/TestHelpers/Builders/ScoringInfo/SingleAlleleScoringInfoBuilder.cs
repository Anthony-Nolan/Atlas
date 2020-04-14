using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo
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