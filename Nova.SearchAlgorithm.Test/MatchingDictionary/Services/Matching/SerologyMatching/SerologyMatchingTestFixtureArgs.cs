using System.Collections;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.SerologyMatching
{
    public class SerologyMatchingTestFixtureArgs : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new object[] { MatchingServiceTestData.Instance.SerologyToSerology };
            yield return new object[] { MatchingServiceTestData.Instance.AllMatchedHla.Where(m => m.HlaType is Serology) };
        }
    }
}
