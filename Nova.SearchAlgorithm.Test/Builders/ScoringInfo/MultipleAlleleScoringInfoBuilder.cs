using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class MultipleAlleleScoringInfoBuilder
    {
        private MultipleAlleleScoringInfo scoringInfo;

        public MultipleAlleleScoringInfoBuilder()
        {
            scoringInfo = new MultipleAlleleScoringInfo(
                new List<SingleAlleleScoringInfo>(),
                new List<SerologyEntry>());
        }
        
        public MultipleAlleleScoringInfoBuilder WithAlleleScoringInfos(IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos)
        {
            scoringInfo = new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                scoringInfo.MatchingSerologies);

            return this;
        }

        public MultipleAlleleScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> matchingSerologies)
        {
            scoringInfo = new MultipleAlleleScoringInfo(
                scoringInfo.AlleleScoringInfos,
                matchingSerologies);

            return this;
        }

        public MultipleAlleleScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}