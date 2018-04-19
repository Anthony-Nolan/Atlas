using NUnit.Framework;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.MatchedHLA
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs))]
    public class MatchedHlaTest : MatchedHlaTestBase<IMatchedHla>
    {
        public MatchedHlaTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void MatchedHlaOnlyContainsMatchLoci()
        {
            var matchCopy = new List<IMatchedHla>(MatchingTypes);
            Assert.IsNotEmpty(matchCopy);

            matchCopy.RemoveAll(m => new List<string> { "A", "B", "C", "DQB1", "DRB1" }.Contains(m.HlaType.MatchLocus));
            Assert.IsEmpty(matchCopy);
        }
    }
}
