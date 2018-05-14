using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypes
{
    [TestFixture]
    public class HlaTypeModelTest
    {
        [Test]
        public void SerologyWmdaLocusConvertedToExpectedMatchLocus()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "1";

            var hlaA = new HlaType("A", hlaName);
            var hlaB = new HlaType("B", hlaName);
            var hlaC = new HlaType("Cw", hlaName);
            var hlaDqb1 = new HlaType("DQ", hlaName);
            var hlaDrb1 = new HlaType("DR", hlaName);

            Assert.AreEqual(hlaA.MatchLocus, MatchLocus.A);
            Assert.AreEqual(hlaB.MatchLocus, MatchLocus.B);
            Assert.AreEqual(hlaC.MatchLocus, MatchLocus.C);
            Assert.AreEqual(hlaDqb1.MatchLocus, MatchLocus.Dqb1);
            Assert.AreEqual(hlaDrb1.MatchLocus, MatchLocus.Drb1);

            Assert.Throws<ArgumentException>(() =>
            {
                var hlaType = new HlaType("DR", "51");
            });
        }

        [Test]
        public void MolecularWmdaLocusConvertedToExpectedMatchLocus()
        {
            // can use a constant hla name here, as we are only testing locus validity
            const string hlaName = "01:01:01:01";

            var hlaA = new HlaType("A*", hlaName);
            var hlaB = new HlaType("B*", hlaName);
            var hlaC = new HlaType("C*", hlaName);
            var hlaDqb1 = new HlaType("DQB1*", hlaName);
            var hlaDrb1 = new HlaType("DRB1*", hlaName);

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
                var hlaDbp1 = new HlaType("DPB1*", hlaName);
                var hlaMadeUpLocus = new HlaType("FOO", hlaName);
            });
        }
    }
}
