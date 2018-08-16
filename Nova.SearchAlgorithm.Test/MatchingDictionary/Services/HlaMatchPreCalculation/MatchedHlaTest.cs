using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public class MatchedHlaTest : MatchedOnTestBase<IMatchedHla>
    {
        private static readonly List<string> ExpectedMatchLoci = new List<string> { "A", "B", "C", "DPB1", "DQB1", "DRB1" };

        [Test]
        public void MatchedHla_ContainsOnlyMatchLoci()
        {        
            MatchedHla.Select(m => m.HlaTyping.MatchLocus.ToString().ToUpper())
                .Distinct()
                .Should()
                .OnlyContain(locus => ExpectedMatchLoci.Contains(locus));
        }
    }
}
