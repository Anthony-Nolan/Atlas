using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public class MatchedHlaTestFixtureArgs
    {
        public static object[] MatchedHla = { HlaMatchingServiceTestData.Instance.MatchedHla };
        public static object[] MatchedSerology = { HlaMatchingServiceTestData.Instance.MatchedHla.OfType<MatchedSerology>() };
        public static object[] MatchedAlleles = { HlaMatchingServiceTestData.Instance.MatchedHla.OfType<MatchedAllele>() };
    }
}
