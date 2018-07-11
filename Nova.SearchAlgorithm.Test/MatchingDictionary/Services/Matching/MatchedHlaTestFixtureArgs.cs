using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public class MatchedHlaTestFixtureArgs
    {
        public static object[] MatchedHla = { HlaMatchingServiceTestData.Instance.MatchedHla };
        public static object[] MatchedSerologies = { HlaMatchingServiceTestData.Instance.MatchedHla.OfType<MatchedSerology>() };
        public static object[] MatchedAlleles = { HlaMatchingServiceTestData.Instance.MatchedHla.OfType<MatchedAllele>() };
    }
}
