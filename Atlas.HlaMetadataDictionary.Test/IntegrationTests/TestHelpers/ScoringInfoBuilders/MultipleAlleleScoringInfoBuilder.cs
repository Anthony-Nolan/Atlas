using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.ScoringInfoBuilders
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