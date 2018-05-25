using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.ConfidentialAllelesTestArgs))]
    public class ConfidentialAlleleTest : WmdaRepositoryTestBase<ConfidentialAllele>
    {
        public ConfidentialAlleleTest(IEnumerable<ConfidentialAllele> confidentialAlleles, IEnumerable<string> matchLoci) 
            : base(confidentialAlleles, matchLoci)
        {
        }

        [Test]
        public void ConfidentialRegexCapturesAllelesAsExpected()
        {
            var confidentialAlleles = new List<ConfidentialAllele>
            {
                new ConfidentialAllele("A*", "02:01:01:28"),
                new ConfidentialAllele("B*", "18:37:02"),
                new ConfidentialAllele("B*", "48:43"),
                new ConfidentialAllele("C*", "06:211N"),
                new ConfidentialAllele("DQB1*", "03:01:01:20"),
                new ConfidentialAllele("DQB1*", "03:23:03")
            };

            Assert.IsTrue(confidentialAlleles.SequenceEqual(HlaTypings));
        }
    }
}
