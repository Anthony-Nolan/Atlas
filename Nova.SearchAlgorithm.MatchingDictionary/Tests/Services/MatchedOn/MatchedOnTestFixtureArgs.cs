using System.Collections;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.MatchedOn
{
    public class MatchedOnTestFixtureArgs : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new object[] { MatchingServiceTestData.Instance.AllelesToPGroups };
            yield return new object[] { MatchingServiceTestData.Instance.SerologyToSerology };
            yield return new object[] { MatchingServiceTestData.Instance.AllMatchedHla };
        }
    }
}
