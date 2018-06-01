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

        [Test]
        public void Serologies_SuccessfullyCaptured()
        {
            var oneDigit = new HlaNom(TypingMethod.Serology, "DQ", "1");
            var twoDigit = new HlaNom(TypingMethod.Serology, "A", "29");
            var threeDigit = new HlaNom(TypingMethod.Serology, "B", "703");
            var fourDigit = new HlaNom(TypingMethod.Serology, "DR", "1404");
            var deletedWithIdentical = new HlaNom(TypingMethod.Serology, "Cw", "11", true, "1");

            Assert.AreEqual(oneDigit, GetSingleWmdaHlaTyping("DQ", "1"));
            Assert.AreEqual(twoDigit, GetSingleWmdaHlaTyping("A", "29"));
            Assert.AreEqual(threeDigit, GetSingleWmdaHlaTyping("B", "703"));
            Assert.AreEqual(fourDigit, GetSingleWmdaHlaTyping("DR", "1404"));
            Assert.AreEqual(deletedWithIdentical, GetSingleWmdaHlaTyping("Cw", "11"));
        }

        [Test]
        public void Serologies_ContainsAllExpectedSerology()
        {
            var str = string.Join("\r\n", HlaTypings
                .OrderBy(s => s.WmdaLocus)
                .ThenBy(s => int.Parse(s.Name))
                .Select(s => $"{s.WmdaLocus}\t{s.Name}")
                .ToList());
            Approvals.Verify(str);
        }
    }
}
