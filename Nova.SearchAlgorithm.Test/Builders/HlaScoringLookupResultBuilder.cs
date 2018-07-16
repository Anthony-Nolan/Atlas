using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup;
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
                TypingMethod.Molecular,
                HlaTypingCategory.Allele,
                new SingleAlleleScoringInfoBuilder().Build()
            );
        }

        public HlaScoringLookupResultBuilder AtLocus(Locus locus)
        {
            result = new HlaScoringLookupResult(locus.ToMatchLocus(), result.LookupName, result.TypingMethod, result.HlaTypingCategory, result.PreCalculatedHlaInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithLookupName(string lookupName)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, lookupName, result.TypingMethod, result.HlaTypingCategory, result.PreCalculatedHlaInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithTypingMethod(TypingMethod typingMethod)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, typingMethod, result.HlaTypingCategory, result.PreCalculatedHlaInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithHlaTypingCategory(HlaTypingCategory typingCategory)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, result.TypingMethod, typingCategory, result.PreCalculatedHlaInfo);
            return this;
        }

        public HlaScoringLookupResultBuilder WithPreCalculatedHlaInfo(IPreCalculatedScoringInfo scoringInfo)
        {
            result = new HlaScoringLookupResult(result.MatchLocus, result.LookupName, result.TypingMethod, result.HlaTypingCategory, scoringInfo);
            return this;
        }
        
        public HlaScoringLookupResult Build()
        {
            return result;
        }
    }
}