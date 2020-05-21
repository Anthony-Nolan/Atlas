using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.ScoringInfo;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    public class HlaScoringLookupResultBuilder
    {
        private HlaScoringLookupResult result;

        public HlaScoringLookupResultBuilder()
        {
            result = new HlaScoringLookupResult(
                Locus.A,
                // Scoring information is cached per-lookup name - so these should be unique by default to avoid cache key collision
                Guid.NewGuid().ToString(),
                HlaTypingCategory.Allele,
                new SingleAlleleScoringInfoBuilder().Build()
            );
        }

        public HlaScoringLookupResultBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringLookupResult(locus, result.LookupName, result.HlaTypingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringLookupResult(result.Locus, lookupName, result.HlaTypingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupNameCategory(HlaTypingCategory hlaTypingCategory)
        {
            result = new HlaScoringLookupResult(result.Locus, result.LookupName, hlaTypingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithHlaScoringInfo(IHlaScoringInfo scoringInfo)
        {
            result = new HlaScoringLookupResult(result.Locus, result.LookupName, result.HlaTypingCategory, scoringInfo);
            return this;
        }
        
        public HlaScoringLookupResult Build()
        {
            return result;
        }
    }
}