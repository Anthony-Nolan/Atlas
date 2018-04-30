using System.Collections;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.AlleleMatching
{
    public class AlleleMatchingTestFixtureArgs : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new object[] { MatchingServiceTestData.Instance.AllelesToPGroups };
            yield return new object[] { MatchingServiceTestData.Instance.AllMatchedHla.Where(m => m.HlaType is Allele) };
        }
    }
}
