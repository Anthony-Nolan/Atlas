using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    [TestFixtureSource(typeof(MolecularTestFixtureArgs), "Args")]
    public class HlaNomAllelesTest : WmdaDataTestBase<HlaNom>
    {
        public HlaNomAllelesTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void HlaNomRegexCapturesAlleleAsExpected()
        {
            var twoField = new HlaNom("A*", "01:26");
            var twoFieldSuffix = new HlaNom("A*", "01:27N");

            var threeField = new HlaNom("B*", "07:05:06");
            var threeFieldSuffix = new HlaNom("C*", "07:491:01N");

            var fourField = new HlaNom("DQB1*", "03:01:01:07");
            var fourFieldSuffix = new HlaNom("C*", "07:01:01:14Q");

            var deletedWithIdentical = new HlaNom("DRB1*", "08:01:03", true, "08:01:01");
            var deletedNoIdentical = new HlaNom("C*", "07:295", true);

            Assert.AreEqual(twoField, GetSingleWmdaHlaType("A*", "01:26"));
            Assert.AreEqual(twoFieldSuffix, GetSingleWmdaHlaType("A*", "01:27N"));

            Assert.AreEqual(threeField, GetSingleWmdaHlaType("B*", "07:05:06"));
            Assert.AreEqual(threeFieldSuffix, GetSingleWmdaHlaType("C*", "07:491:01N"));

            Assert.AreEqual(fourField, GetSingleWmdaHlaType("DQB1*", "03:01:01:07"));
            Assert.AreEqual(fourFieldSuffix, GetSingleWmdaHlaType("C*", "07:01:01:14Q"));

            Assert.AreEqual(deletedWithIdentical, GetSingleWmdaHlaType("DRB1*", "08:01:03"));
            Assert.AreEqual(deletedNoIdentical, GetSingleWmdaHlaType("C*", "07:295"));
        }
    }
}
