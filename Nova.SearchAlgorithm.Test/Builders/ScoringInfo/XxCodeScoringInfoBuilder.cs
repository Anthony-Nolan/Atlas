using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class XxCodeScoringInfoBuilder
    {
        private XxCodeScoringInfo scoringInfo;

        public XxCodeScoringInfoBuilder()
        {
            scoringInfo = new XxCodeScoringInfo(
                new List<string>(), 
                new List<string>(), 
                new List<SerologyEntry>()
                );
        }
        
        public XxCodeScoringInfoBuilder WithMatchingPGroups(IEnumerable<string> pGroups)
        {
            scoringInfo = new XxCodeScoringInfo(pGroups, scoringInfo.MatchingGGroups, scoringInfo.MatchingSerologies);
            return this;
        }
        
        public XxCodeScoringInfoBuilder WithMatchingGGroups(IEnumerable<string> gGroups)
        {
            scoringInfo = new XxCodeScoringInfo(scoringInfo.MatchingPGroups, gGroups, scoringInfo.MatchingSerologies);
            return this;
        }
        
        public XxCodeScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> matchingSerologies)
        {
            scoringInfo = new XxCodeScoringInfo(scoringInfo.MatchingPGroups, scoringInfo.MatchingGGroups, matchingSerologies);
            return this;
        }

        public XxCodeScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}