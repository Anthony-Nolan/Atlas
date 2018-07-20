using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class MultipleAlleleScoringInfoBuilder
    {
        private MultipleAlleleScoringInfo scoringInfo;

        public MultipleAlleleScoringInfoBuilder()
        {
            scoringInfo = new MultipleAlleleScoringInfo(new List<SingleAlleleScoringInfo>());
        }
        
        public MultipleAlleleScoringInfoBuilder WithAlleleScoringInfos(IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos)
        {
            scoringInfo = new MultipleAlleleScoringInfo(alleleScoringInfos);
            return this;
        }

        public MultipleAlleleScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}