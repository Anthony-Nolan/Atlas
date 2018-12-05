using Nova.SearchAlgorithm.Common.Models;
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

        [TestCase("A", Locus.A)]
        [TestCase("B", Locus.B)]
        [TestCase("Cw", Locus.C)]
        [TestCase("DQ", Locus.Dqb1)]
        [TestCase("DR", Locus.Drb1)]
        public void HlaTyping_WhenSerologyTyping_LocusConvertedToMatchLocus(string serologyLocus, Locus expectedLocus)
        {
            var actualHlaTyping = new HlaTyping(TypingMethod.Serology, serologyLocus, HlaName);

            Assert.AreEqual(expectedLocus, actualHlaTyping.Locus);
        }

        [TestCase("A*", Locus.A)]
        [TestCase("B*", Locus.B)]
        [TestCase("C*", Locus.C)]
        [TestCase("DPB1*", Locus.Dpb1)]
        [TestCase("DQB1*", Locus.Dqb1)]
        [TestCase("DRB1*", Locus.Drb1)]
        public void HlaTyping_WhenMolecularTyping_LocusConvertedToMatchLocus(string molecularLocus, Locus expectedLocus)
        {
            var actualHlaTyping = new HlaTyping(TypingMethod.Molecular, molecularLocus, HlaName);

            Assert.AreEqual(expectedLocus, actualHlaTyping.Locus);
        }

        [TestCase(TypingMethod.Molecular, "TAP1*")]
        [TestCase(TypingMethod.Serology, "MadeUpLocus")]
        public void HlaTyping_WhenNotPermittedLocus_RaisesPermittedLocusException(TypingMethod typingMethod, string nonPermittedLocus)
        {
            Assert.Throws<LocusNameException>(() =>
            {
                var hlaTyping = new HlaTyping(typingMethod, nonPermittedLocus, HlaName);
            });
        }
    }
}
