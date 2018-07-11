using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.HlaNomSerologiesTestArgs))]
    [UseReporter(typeof(DiffReporter))]
    public class SerologiesTest : WmdaRepositoryTestBase<HlaNom>
    {
        public SerologiesTest(IEnumerable<HlaNom> hlaNomSerologies, IEnumerable<string> matchLoci)
            : base(hlaNomSerologies, matchLoci)
        {
        }

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

        [TestCase("Cw", "11", "1")]
        public void WmdaDataRepository_WhenDeletedSerology_SuccessfullyCaptured(
            string locus, string serologyName, string identicalHla)
        {
            var expectedSerology = new HlaNom(TypingMethod.Serology, locus, serologyName, true, identicalHla);

            var actualSerology = GetSingleWmdaHlaTyping(locus, serologyName);
            
            Assert.AreEqual(expectedSerology, actualSerology);
        }

        [Test]
        public void WmdaDataRepository_SerologiesCollection_ContainsAllExpectedSerologies()
        {
            var str = string.Join("\r\n", WmdaHlaTypings
                .OrderBy(s => s.Locus)
                .ThenBy(s => int.Parse(s.Name))
                .Select(s => $"{s.Locus}\t{s.Name}")
                .ToList());
            Approvals.Verify(str);
        }
    }
}
