using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedHla))]
    public class MatchedHlaTest : MatchedOnTestBase<IMatchedHla>
    {
        public MatchedHlaTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void MatchedHlaOnlyContainsMatchLoci()
        {
            var matchCopy = new List<IMatchedHla>(MatchingTypes);
            Assert.IsNotEmpty(matchCopy);

            matchCopy.RemoveAll(m => new List<string> { "A", "B", "C", "DQB1", "DRB1" }.Contains(m.HlaType.MatchLocus.ToString().ToUpper()));
            Assert.IsEmpty(matchCopy);
        }
    }
}
