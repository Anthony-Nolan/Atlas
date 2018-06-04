using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    [TestFixture]
    public class HlaTypingModelTest
    {
        [Test]
        public void SerologyWmdaLocusConvertedToExpectedMatchLocus()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "1";

            var hlaA = new HlaTyping("A", hlaName);
            var hlaB = new HlaTyping("B", hlaName);
            var hlaC = new HlaTyping("Cw", hlaName);
            var hlaDqb1 = new HlaTyping("DQ", hlaName);
            var hlaDrb1 = new HlaTyping("DR", hlaName);

            Assert.AreEqual(hlaA.MatchLocus, MatchLocus.A);
            Assert.AreEqual(hlaB.MatchLocus, MatchLocus.B);
            Assert.AreEqual(hlaC.MatchLocus, MatchLocus.C);
            Assert.AreEqual(hlaDqb1.MatchLocus, MatchLocus.Dqb1);
            Assert.AreEqual(hlaDrb1.MatchLocus, MatchLocus.Drb1);
        }

        [Test]
        public void MolecularWmdaLocusConvertedToExpectedMatchLocus()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "01:01:01:01";

            var hlaA = new HlaTyping("A*", hlaName);
            var hlaB = new HlaTyping("B*", hlaName);
            var hlaC = new HlaTyping("C*", hlaName);
            var hlaDqb1 = new HlaTyping("DQB1*", hlaName);
            var hlaDrb1 = new HlaTyping("DRB1*", hlaName);

            Assert.AreEqual(hlaA.MatchLocus, MatchLocus.A);
            Assert.AreEqual(hlaB.MatchLocus, MatchLocus.B);
            Assert.AreEqual(hlaC.MatchLocus, MatchLocus.C);
            Assert.AreEqual(hlaDqb1.MatchLocus, MatchLocus.Dqb1);
            Assert.AreEqual(hlaDrb1.MatchLocus, MatchLocus.Drb1);
        }

        [Test]
        public void InvalidLocusRaisesException()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "01:01:01:01";

            Assert.Throws<ArgumentException>(() =>
            {
                var hlaDbp1 = new HlaTyping("DPB1*", hlaName);
                var hlaMadeUpLocus = new HlaTyping("FOO", hlaName);
            });
        }
    }
}
