using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class HlaScoringLookupResultBuilder
    {
        private HlaScoringLookupResult result;

        public HlaScoringLookupResultBuilder()
        {
            result = new HlaScoringLookupResult(
                Locus.A,
                "lookup-name",
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