using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
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
            var matchCopy = new List<IMatchedHla>(MatchedHlaTypings);
            Assert.IsNotEmpty(matchCopy);

            matchCopy.RemoveAll(m => new List<string> { "A", "B", "C", "DQB1", "DRB1" }.Contains(m.HlaTyping.MatchLocus.ToString().ToUpper()));
            Assert.IsEmpty(matchCopy);
        }
    }
}
