using System.Collections;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.MatchedOn
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
