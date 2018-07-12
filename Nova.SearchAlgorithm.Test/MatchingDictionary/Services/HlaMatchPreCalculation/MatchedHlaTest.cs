using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public class MatchedHlaTest : MatchedOnTestBase<IMatchedHla>
    {
        [Test]
        public void MatchedHlaOnlyContainsMatchLoci()
        {
            var matchCopy = new List<IMatchedHla>(MatchedHla);
            Assert.IsNotEmpty(matchCopy);

            matchCopy.RemoveAll(m => new List<string> { "A", "B", "C", "DQB1", "DRB1" }.Contains(m.HlaTyping.MatchLocus.ToString().ToUpper()));
            Assert.IsEmpty(matchCopy);
        }
    }
}
