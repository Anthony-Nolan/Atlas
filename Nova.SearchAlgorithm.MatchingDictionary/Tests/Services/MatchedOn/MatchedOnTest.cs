using NUnit.Framework;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.MatchedOn
{
    [TestFixtureSource(typeof(MatchedOnTestFixtureArgs))]
    public class MatchedOnTest : MatchedOnTestBase<IMatchedOn>
    {
        public MatchedOnTest(IEnumerable<IMatchedOn> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void MatchedHlaOnlyContainsMatchLoci()
        {
            var matchCopy = new List<IMatchedOn>(MatchingTypes);
            Assert.IsNotEmpty(matchCopy);

            matchCopy.RemoveAll(m => new List<string> { "A", "B", "C", "DQB1", "DRB1" }.Contains(m.HlaType.MatchLocus));
            Assert.IsEmpty(matchCopy);
        }
    }
}
