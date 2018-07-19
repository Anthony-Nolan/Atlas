using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class HlaScoringLookupResultBuilder
    {
        private HlaScoringLookupResult result;

        public HlaScoringLookupResultBuilder()
        {
            result = new HlaScoringLookupResult(
                MatchLocus.A,
                "lookup-name",
                LookupResultCategory.OriginalAllele,
                new SingleAlleleScoringInfoBuilder().Build()
            );
        }

        public HlaScoringLookupResultBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringLookupResult(locus.ToMatchLocus(), result.LookupName, result.LookupResultCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, lookupName, result.LookupResultCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupResultCategory(LookupResultCategory lookupResultCategory)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, lookupResultCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithHlaScoringInfo(IHlaScoringInfo scoringInfo)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, result.LookupResultCategory, scoringInfo);
            return this;
        }
        
        public HlaScoringLookupResult Build()
        {
            return result;
        }
    }
}