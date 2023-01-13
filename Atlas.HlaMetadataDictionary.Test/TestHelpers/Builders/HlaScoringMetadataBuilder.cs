using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using System;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders
{
    internal class HlaScoringMetadataBuilder
    {
        private HlaScoringMetadata result;

        public HlaScoringMetadataBuilder()
        {
            result = new HlaScoringMetadata(
                Locus.A,
                // Scoring information is cached per-lookup name - so these should be unique by default to avoid cache key collision
                Guid.NewGuid().ToString(),
                new SingleAlleleScoringInfoBuilder().Build(),
                TypingMethod.Molecular
            );
        }

        public HlaScoringMetadataBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringMetadata(locus, result.LookupName, result.HlaScoringInfo, result.TypingMethod);
            return this;
        }

        public HlaScoringMetadataBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringMetadata(result.Locus, lookupName, result.HlaScoringInfo, result.TypingMethod);
            return this;
        }

        public HlaScoringMetadataBuilder WithHlaScoringInfo(IHlaScoringInfo scoringInfo)
        {
            result = new HlaScoringMetadata(result.Locus, result.LookupName, scoringInfo, result.TypingMethod);
            return this;
        }
        
        public HlaScoringMetadataBuilder WithTypingMethod(TypingMethod typingMethod)
        {
            result = new HlaScoringMetadata(result.Locus, result.LookupName, result.HlaScoringInfo, typingMethod);
            return this;
        }

        public HlaScoringMetadata Build()
        {
            return result;
        }
    }
}