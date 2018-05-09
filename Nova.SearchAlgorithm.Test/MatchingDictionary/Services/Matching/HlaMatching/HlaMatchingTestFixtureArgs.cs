using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.HlaMatching
{
    public class HlaMatchingTestFixtureArgs
    {
        public static object[] MatchedHla = { MatchingServiceTestData.Instance.AllMatchedHla };
        public static object[] MatchedSerology = { MatchingServiceTestData.Instance.AllMatchedHla.OfType<MatchedSerology>() };
        public static object[] MatchedAlleles = { MatchingServiceTestData.Instance.AllMatchedHla.OfType<MatchedAllele>() };
    }
}
