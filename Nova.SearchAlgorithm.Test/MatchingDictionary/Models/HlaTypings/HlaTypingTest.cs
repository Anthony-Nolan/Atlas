using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    [TestFixture]
    public class HlaTypingTest
    {
        // can use a constant hla name, as we are only testing locus validity
        private const string HlaName = "01:01";

        [TestCase("A", MatchLocus.A)]
        [TestCase("B", MatchLocus.B)]
        [TestCase("Cw", MatchLocus.C)]
        [TestCase("DQ", MatchLocus.Dqb1)]
        [TestCase("DR", MatchLocus.Drb1)]
        public void HlaTyping_WhenSerologyTyping_LocusConvertedToMatchLocus(string locus, MatchLocus expectedMatchLocus)
        {
            var actualHlaTyping = new HlaTyping(TypingMethod.Serology, locus, HlaName);

            Assert.AreEqual(expectedMatchLocus, actualHlaTyping.MatchLocus);
        }

        [TestCase("A*", MatchLocus.A)]
        [TestCase("B*", MatchLocus.B)]
        [TestCase("C*", MatchLocus.C)]
        [TestCase("DPB1*", MatchLocus.Dpb1)]
        [TestCase("DQB1*", MatchLocus.Dqb1)]
        [TestCase("DRB1*", MatchLocus.Drb1)]
        public void HlaTyping_WhenMolecularTyping_LocusConvertedToMatchLocus(string locus, MatchLocus expectedMatchLocus)
        {
            var actualHlaTyping = new HlaTyping(TypingMethod.Molecular, locus, HlaName);

            Assert.AreEqual(expectedMatchLocus, actualHlaTyping.MatchLocus);
        }

        [TestCase(TypingMethod.Molecular, "TAP1*")]
        [TestCase(TypingMethod.Serology, "MadeUpLocus")]
        public void HlaTyping_WhenNotPermittedLocus_RaisesPermittedLocusException(TypingMethod typingMethod, string nonPermittedLocus)
        {
            Assert.Throws<PermittedLocusException>(() =>
            {
                var hlaTyping = new HlaTyping(typingMethod, nonPermittedLocus, HlaName);
            });
        }
    }
}
