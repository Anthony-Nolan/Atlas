using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    [TestFixtureSource(typeof(MolecularTestFixtureArgs), "Args")]
    public class ConfidentialTest : WmdaDataTestBase<Confidential>
    {
        public ConfidentialTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci) 
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void ConfidentialRegexCapturesAllelesAsExpected()
        {
            var confidentialAlleles = new List<Confidential>
            {
                new Confidential("A*", "02:01:01:28"),
                new Confidential("B*", "18:37:02"),
                new Confidential("B*", "48:43"),
                new Confidential("DQB1*", "03:01:01:20"),
                new Confidential("DQB1*", "03:23:03")
            };

            Assert.IsTrue(confidentialAlleles.SequenceEqual(AllHlaTypes));
        }
    }
}
