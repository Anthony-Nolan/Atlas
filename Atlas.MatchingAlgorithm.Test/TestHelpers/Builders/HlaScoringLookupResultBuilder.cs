using System;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo;

namespace Atlas.MatchingAlgorithm.Test.Builders
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
                LookupNameCategory.OriginalAllele,
                new SingleAlleleScoringInfoBuilder().Build()
            );
        }

        public HlaScoringLookupResultBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringLookupResult(locus, result.LookupName, result.LookupNameCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringLookupResult(result.Locus, lookupName, result.LookupNameCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupNameCategory(LookupNameCategory lookupNameCategory)
        {
            result = new HlaScoringLookupResult(result.Locus, result.LookupName, lookupNameCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithHlaScoringInfo(IHlaScoringInfo scoringInfo)
        {
            result = new HlaScoringLookupResult(result.Locus, result.LookupName, result.LookupNameCategory, scoringInfo);
            return this;
        }
        
        public HlaScoringLookupResult Build()
        {
            return result;
        }
    }
}