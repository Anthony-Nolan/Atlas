using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.HlaMatching
{
    public class HlaMatchingTestFixtureArgs
    {
        public static object[] MatchedHla = { MatchingServiceTestData.Instance.AllMatchedHla };
        public static object[] MatchedSerology = { MatchingServiceTestData.Instance.AllMatchedHla.Where(m => !(m is MatchedAllele)) };
        public static object[] MatchedAlleles = { MatchingServiceTestData.Instance.AllMatchedHla.OfType<MatchedAllele>() };
    }
}
