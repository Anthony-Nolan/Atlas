using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatching
{
    [TestFixtureSource(typeof(HlaMatchingTestFixtureArgs), "MatchedSerology")]
    [UseReporter(typeof(NUnitReporter))]
    public class SerologyToPGroupsMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        public SerologyToPGroupsMatchingTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void ValidSerologyHaveAtLeastOnePGroup()
        {
            var pGroupCounts = MatchingTypes
                .Where(m => !m.HlaType.IsDeleted && m.HlaType is Serology)
                .Select(m => new { m.HlaType, PGroupCount = m.MatchingPGroups.Count() })
                .ToList();

            Assert.IsTrue(pGroupCounts.All(p => p.PGroupCount > 0));
        }

        [Test]
        public void BroadWhereSplitHasAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("A", "9"));
        }

        [Test]
        public void BroadWhereSplitHasNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("DQB1", "1"));
        }

        [Test]
        public void BroadHasSplitAndAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "21"));
        }

        [Test]
        public void SplitHasAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "51"));
        }

        [Test]
        public void SplitNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("C", "10"));
        }

        [Test]
        public void AssociatedWithSplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "3902"));
        }

        [Test]
        public void AssociatedWithBroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "4005"));
        }

        [Test]
        public void AssociatedWithNotSplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("DRB1", "103"));
        }

        [Test]
        public void NotSplitWithAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("A", "2"));
        }

        [Test]
        public void NotSplitNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("A", "1"));
        }

        [Test]
        public void DeletedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("C", "11"));
        }

        [Test]
        public void B15BroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "15"));
        }

        [Test]
        public void B15SplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "62"));
        }

        [Test]
        public void B70BroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "70"));
        }

        [Test]
        public void B70SplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString("B", "72"));
        }

        [Test]
        public void B15And70ShareMatchingPGroups()
        {
            var b15 = GetSingleMatchingType("B", "15").MatchingPGroups;
            var b70 = GetSingleMatchingType("B", "70").MatchingPGroups;

            Approvals.Verify(string.Join("\r\n", b15.Intersect(b70).OrderBy(p => p)));
        }

        private string GetPGroupsAsString(string locus, string serologyName)
        {
            return string.Join("\r\n",
                GetSingleMatchingType(locus, serologyName)
                .MatchingPGroups
                .OrderBy(p => p));
        }
    }
}
