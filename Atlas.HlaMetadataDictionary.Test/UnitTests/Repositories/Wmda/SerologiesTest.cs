using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    [UseReporter(typeof(DiffReporter))]
    internal class SerologiesTest : WmdaRepositoryTestBase<HlaNom>
    {
        protected override IEnumerable<HlaNom> SelectTestDataTypings(WmdaDataset dataset) => dataset.Serologies;
        protected override string[] ApplicableLoci => SerologyLoci;

        [TestCase("DQ", "1")]
        [TestCase("A", "29")]
        [TestCase("B", "703")]
        [TestCase("DR", "1404")]
        public void WmdaDataRepository_WhenValidSerology_SuccessfullyCaptured(string locus, string serologyName)
        {
            var expectedSerology = new HlaNom(TypingMethod.Serology, locus, serologyName);

            var actualSerology = GetSingleWmdaHlaTyping(locus, serologyName);
            
            Assert.AreEqual(expectedSerology, actualSerology);
        }

        [Test]
        public void WmdaDataRepository_WhenDeletedSerology_SuccessfullyCaptured()
        {
            const string locus = "Cw";
            const string serologyName = "11";
            const string identicalHla = "1";

            var expectedSerology = new HlaNom(TypingMethod.Serology, locus, serologyName, true, identicalHla);

            var actualSerology = GetSingleWmdaHlaTyping(locus, serologyName);
            
            Assert.AreEqual(expectedSerology, actualSerology);
        }

        [Test]
        public void WmdaDataRepository_SerologiesCollection_ContainsAllExpectedSerologies()
        {
            var str = WmdaHlaTypings
                .OrderBy(s => s.TypingLocus)
                .ThenBy(s => int.Parse(s.Name))
                .Select(s => $"{s.TypingLocus}\t{s.Name}")
                .StringJoinWithNewline();
            Approvals.Verify(str);
        }
    }
}
