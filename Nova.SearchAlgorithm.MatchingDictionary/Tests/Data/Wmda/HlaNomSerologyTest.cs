using System;
using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Data.Wmda
{
    [TestFixtureSource(typeof(SerologyTestFixtureArgs), "Args")]
    [UseReporter(typeof(DiffReporter))]
    public class HlaNomSerologyTest : WmdaDataTestBase<HlaNom>
    {
        public HlaNomSerologyTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void HlaNomRegexCapturesSerologyAsExpected()
        {
            var oneDigit = new HlaNom("DQ", "1");
            var twoDigit = new HlaNom("A", "29");
            var threeDigit = new HlaNom("B", "703");
            var fourDigit = new HlaNom("DR", "1404");
            var deletedWithIdentical = new HlaNom("Cw", "11", true, "1");

            Assert.AreEqual(oneDigit, GetSingleWmdaHlaType("DQ", "1"));
            Assert.AreEqual(twoDigit, GetSingleWmdaHlaType("A", "29"));
            Assert.AreEqual(threeDigit, GetSingleWmdaHlaType("B", "703"));
            Assert.AreEqual(fourDigit, GetSingleWmdaHlaType("DR", "1404"));
            Assert.AreEqual(deletedWithIdentical, GetSingleWmdaHlaType("Cw", "11"));
        }

        [Test]
        public void HlaNomContainsAllExpectedSerology()
        {
            var str = string.Join("\r\n", AllHlaTypes
                .OrderBy(s => s.WmdaLocus)
                .ThenBy(s => int.Parse(s.Name))
                .Select(s => $"{s.WmdaLocus}\t{s.Name}")
                .ToList());
            Approvals.Verify(str);
        }
    }
}
