using System.Collections;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.SerologyMatching
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
