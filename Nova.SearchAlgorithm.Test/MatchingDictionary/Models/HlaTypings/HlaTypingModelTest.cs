using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
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

            var hlaA = new HlaTyping(TypingMethod.Serology, "A", hlaName);
            var hlaB = new HlaTyping(TypingMethod.Serology, "B", hlaName);
            var hlaC = new HlaTyping(TypingMethod.Serology, "Cw", hlaName);
            var hlaDqb1 = new HlaTyping(TypingMethod.Serology, "DQ", hlaName);
            var hlaDrb1 = new HlaTyping(TypingMethod.Serology, "DR", hlaName);

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

            var hlaA = new HlaTyping(TypingMethod.Molecular, "A*", hlaName);
            var hlaB = new HlaTyping(TypingMethod.Molecular, "B*", hlaName);
            var hlaC = new HlaTyping(TypingMethod.Molecular, "C*", hlaName);
            var hlaDqb1 = new HlaTyping(TypingMethod.Molecular, "DQB1*", hlaName);
            var hlaDrb1 = new HlaTyping(TypingMethod.Molecular, "DRB1*", hlaName);

            Assert.AreEqual(hlaA.MatchLocus, MatchLocus.A);
            Assert.AreEqual(hlaB.MatchLocus, MatchLocus.B);
            Assert.AreEqual(hlaC.MatchLocus, MatchLocus.C);
            Assert.AreEqual(hlaDqb1.MatchLocus, MatchLocus.Dqb1);
            Assert.AreEqual(hlaDrb1.MatchLocus, MatchLocus.Drb1);
        }

        [Test]
        public void InvalidLocus_RaisesPermittedLocusException()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "01:01:01:01";

            Assert.Throws<PermittedLocusException>(() =>
            {
                var tap1Locus = new HlaTyping(TypingMethod.Molecular, "TAP1*", hlaName);
                var hlaMadeUpLocus = new HlaTyping(TypingMethod.Serology, "FOO", hlaName);
            });
        }
    }
}
