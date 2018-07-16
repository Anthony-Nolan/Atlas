using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    [TestFixture]
    public class HlaMatchingDataConverterTest : 
        MatchedHlaDataConverterTestBase<HlaMatchingDataConverter>
    {
        protected override IHlaLookupResult BuildExpectedHlaLookupResult(MatchedAllele matchedAllele, string alleleName)
        {
            return new HlaMatchingLookupResult(matchedAllele, alleleName);
        }
        
        // All tests are run in the base class
    }
}
