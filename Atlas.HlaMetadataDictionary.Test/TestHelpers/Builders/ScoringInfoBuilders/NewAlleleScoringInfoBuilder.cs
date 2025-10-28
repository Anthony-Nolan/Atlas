using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders
{
    public class NewAlleleScoringInfoBuilder
    {
        private NewAlleleScoringInfo scoringInfo;

        public NewAlleleScoringInfoBuilder()
        {
            scoringInfo = new NewAlleleScoringInfo();
        }

        public NewAlleleScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}
