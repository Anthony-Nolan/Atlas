using System.Collections;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.MatchedHLA
{
    public class MatchedHlaTestFixtureArgs : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new object[] { MatchingServiceTestData.Instance.AllelesToPGroups };
            yield return new object[] { MatchingServiceTestData.Instance.SerologyToSerology };
            yield return new object[] { MatchingServiceTestData.Instance.AllMatchedHla };
        }
    }
}
