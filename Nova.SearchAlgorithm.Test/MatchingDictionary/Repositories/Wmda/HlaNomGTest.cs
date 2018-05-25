using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.HlaNomGTestArgs))]
    public class HlaNomGTest : WmdaRepositoryTestBase<HlaNomG>
    {
        public HlaNomGTest(IEnumerable<HlaNomG> hlaNomG, IEnumerable<string> matchLoci)
            : base(hlaNomG, matchLoci)
        {
        }

        [Test]
        public void HolNomGRegexCapturesGGroupAsExpected()
        {
            var alleleNoGGroup = new HlaNomG("A*", "01:01:02", new List<string> { "01:01:02" });
            var alleleSuffixNoGGroup = new HlaNomG("B*", "37:33N", new List<string> { "37:33N" });

            var singleAlleleGGroup = new HlaNomG("DRB1*", "11:11:01G", new List<string> { "11:11:01" });
            var sameSubtypeGGroup = new HlaNomG("C*", "02:14:01G", new List<string> { "02:14:01", "02:14:02" });
            var crossSubtypeGGroup = new HlaNomG("C*", "01:03:01G", new List<string> { "01:03", "01:24" });
            var alleleSuffixGGroup = new HlaNomG("DQB1*", "05:04:01G", new List<string> { "05:04", "05:132Q" });

            var longMixedGGroup = new HlaNomG("DQB1*", "05:02:01G",
                new List<string> {
                    "05:02:01:01", "05:02:01:02", "05:02:01:03",
                    "05:02:03", "05:02:07", "05:02:11",
                    "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                    "05:87Q", "05:90N",
                    "05:102", "05:106", "05:136" });

            Assert.AreEqual(alleleNoGGroup, GetSingleWmdaHlaTyping("A*", "01:01:02"));
            Assert.AreEqual(alleleSuffixNoGGroup, GetSingleWmdaHlaTyping("B*", "37:33N"));

            Assert.AreEqual(singleAlleleGGroup, GetSingleWmdaHlaTyping("DRB1*", "11:11:01G"));
            Assert.AreEqual(sameSubtypeGGroup, GetSingleWmdaHlaTyping("C*", "02:14:01G"));
            Assert.AreEqual(crossSubtypeGGroup, GetSingleWmdaHlaTyping("C*", "01:03:01G"));
            Assert.AreEqual(alleleSuffixGGroup, GetSingleWmdaHlaTyping("DQB1*", "05:04:01G"));

            Assert.AreEqual(longMixedGGroup, GetSingleWmdaHlaTyping("DQB1*", "05:02:01G"));
        }
    }
}
