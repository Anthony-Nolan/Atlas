using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.HlaNomAllelesTestArgs))]
    public class AllelesTest : WmdaRepositoryTestBase<HlaNom>
    {
        public AllelesTest(IEnumerable<HlaNom> hlaNomAlleles, IEnumerable<string> matchLoci)
            : base(hlaNomAlleles, matchLoci)
        {
        }

        [Test]
        public void Alleles_SuccessfullyCaptured()
        {
            var twoField = new HlaNom(TypingMethod.Molecular, "A*", "01:26");
            var twoFieldSuffix = new HlaNom(TypingMethod.Molecular, "A*", "01:27N");

            var threeField = new HlaNom(TypingMethod.Molecular, "B*", "07:05:06");
            var threeFieldSuffix = new HlaNom(TypingMethod.Molecular, "C*", "07:491:01N");

            var fourField = new HlaNom(TypingMethod.Molecular, "DQB1*", "03:01:01:07");
            var fourFieldSuffix = new HlaNom(TypingMethod.Molecular, "C*", "07:01:01:14Q");

            var deletedWithIdentical = new HlaNom(TypingMethod.Molecular, "DRB1*", "08:01:03", true, "08:01:01");
            var deletedNoIdentical = new HlaNom(TypingMethod.Molecular, "C*", "07:295", true);

            Assert.AreEqual(twoField, GetSingleWmdaHlaTyping("A*", "01:26"));
            Assert.AreEqual(twoFieldSuffix, GetSingleWmdaHlaTyping("A*", "01:27N"));

            Assert.AreEqual(threeField, GetSingleWmdaHlaTyping("B*", "07:05:06"));
            Assert.AreEqual(threeFieldSuffix, GetSingleWmdaHlaTyping("C*", "07:491:01N"));

            Assert.AreEqual(fourField, GetSingleWmdaHlaTyping("DQB1*", "03:01:01:07"));
            Assert.AreEqual(fourFieldSuffix, GetSingleWmdaHlaTyping("C*", "07:01:01:14Q"));

            Assert.AreEqual(deletedWithIdentical, GetSingleWmdaHlaTyping("DRB1*", "08:01:03"));
            Assert.AreEqual(deletedNoIdentical, GetSingleWmdaHlaTyping("C*", "07:295"));
        }
    }
}
