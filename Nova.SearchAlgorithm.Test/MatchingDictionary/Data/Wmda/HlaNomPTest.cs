using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    [TestFixtureSource(typeof(MolecularTestFixtureArgs), "Args")]
    public class HlaNomPTest : WmdaDataTestBase<HlaNomP>
    {
        public HlaNomPTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void HolNomPRegexCapturesPGroupAsExpected()
        {
            var alleleNoPGroup = new HlaNomP("B*", "08:100", new List<string> { "08:100" });
            var alleleSuffixNoPGroup = new HlaNomP("A*", "30:14L", new List<string> { "30:14L" });

            var sameSubtypePGroup = new HlaNomP("DRB1*", "03:02P", new List<string> { "03:02:01", "03:02:02", "03:02:03" });
            var crossSubtypePGroup = new HlaNomP("C*", "03:14P", new List<string> { "03:14", "03:361" });
            var crossFamilyPGroup = new HlaNomP("A*", "02:65P", new List<string> { "02:65", "74:21" });
            var alleleSuffixPGroup = new HlaNomP("DQB1*", "05:04P", new List<string> { "05:04", "05:132Q" });

            var longMixedPGroup = new HlaNomP("DQB1*", "05:02P",
                new List<string> {
                    "05:02:01:01", "05:02:01:02", "05:02:01:03",
                    "05:02:02", "05:02:03", "05:02:04", "05:02:05", "05:02:06", "05:02:07", "05:02:08", "05:02:09", "05:02:10", "05:02:11", "05:02:12", "05:02:13",
                    "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                    "05:87Q",
                    "05:102", "05:106", "05:136" });

            Assert.AreEqual(alleleNoPGroup, GetSingleWmdaHlaType("B*", "08:100"));
            Assert.AreEqual(alleleSuffixNoPGroup, GetSingleWmdaHlaType("A*", "30:14L"));

            Assert.AreEqual(sameSubtypePGroup, GetSingleWmdaHlaType("DRB1*", "03:02P"));
            Assert.AreEqual(crossSubtypePGroup, GetSingleWmdaHlaType("C*", "03:14P"));
            Assert.AreEqual(crossFamilyPGroup, GetSingleWmdaHlaType("A*", "02:65P"));
            Assert.AreEqual(alleleSuffixPGroup, GetSingleWmdaHlaType("DQB1*", "05:04P"));

            Assert.AreEqual(longMixedPGroup, GetSingleWmdaHlaType("DQB1*", "05:02P"));
        }
    }
}
