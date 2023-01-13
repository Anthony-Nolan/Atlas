using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Models.HlaTypings
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
        public void HlaTyping_WhenSerologyTyping_SerologyLocusConvertedToLocus(string serologyLocus, Locus expectedLocus)
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
        public void HlaTyping_WhenMolecularTyping_MolecularLocusConvertedToLocus(string molecularLocus, Locus expectedLocus)
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
