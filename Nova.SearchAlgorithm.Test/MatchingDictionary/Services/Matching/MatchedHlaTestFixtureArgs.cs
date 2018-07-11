using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public class MatchedHlaTestFixtureArgs
    {
        public static object[] MatchedHla = { HlaMatchPreCalculationServiceTestData.Instance.MatchedHla };
        public static object[] MatchedSerologies = { HlaMatchPreCalculationServiceTestData.Instance.MatchedHla.OfType<MatchedSerology>() };
        public static object[] MatchedAlleles = { HlaMatchPreCalculationServiceTestData.Instance.MatchedHla.OfType<MatchedAllele>() };
    }
}
