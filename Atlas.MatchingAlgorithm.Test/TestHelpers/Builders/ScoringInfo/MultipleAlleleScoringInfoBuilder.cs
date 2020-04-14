using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo
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