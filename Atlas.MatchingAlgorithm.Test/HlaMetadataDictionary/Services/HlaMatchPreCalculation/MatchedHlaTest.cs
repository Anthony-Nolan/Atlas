using FluentAssertions;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaMatchPreCalculation
{
    public class MatchedHlaTest : MatchedOnTestBase<IMatchedHla>
    {
        private static readonly List<string> ExpectedMatchLoci = new List<string> { "A", "B", "C", "DPB1", "DQB1", "DRB1" };

        [Test]
        public void MatchedHla_ContainsOnlyMatchLoci()
        {        
            MatchedHla.Select(m => m.HlaTyping.Locus.ToString().ToUpper())
                .Distinct()
                .Should()
                .OnlyContain(locus => ExpectedMatchLoci.Contains(locus));
        }
    }
}
