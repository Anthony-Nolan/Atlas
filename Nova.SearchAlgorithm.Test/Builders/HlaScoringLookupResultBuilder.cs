using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
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
                HlaTypingCategory.Allele,
                new SingleAlleleScoringInfoBuilder().Build()
            );
        }

        public HlaScoringLookupResultBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringLookupResult(locus.ToMatchLocus(), result.LookupName, result.HlaTypingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, lookupName, result.HlaTypingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithHlaTypingCategory(HlaTypingCategory typingCategory)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, typingCategory, result.HlaScoringInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithPreCalculatedHlaInfo(IHlaScoringInfo scoringInfo)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, result.HlaTypingCategory, scoringInfo);
            return this;
        }
        
        public HlaScoringLookupResult Build()
        {
            return result;
        }
    }
}