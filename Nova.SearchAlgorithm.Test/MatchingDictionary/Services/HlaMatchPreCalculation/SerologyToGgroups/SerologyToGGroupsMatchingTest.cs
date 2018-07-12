using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyToGgroups
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedSerologies))]
    [UseReporter(typeof(NUnitReporter))]
    [ApprovalTests.Namers.UseApprovalSubdirectory("../../../../Resources/MDPreCalc")]
    public class SerologyToGGroupsMatchingTest : MatchedOnTestBase<MatchedSerology>
    {
        public SerologyToGGroupsMatchingTest(IEnumerable<MatchedSerology> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void ValidSerologyHaveAtLeastOneGGroup()
        {
            var gGroupCounts = MatchedHlaTypings
                .Where(m => !m.HlaTyping.IsDeleted && m.HlaTyping is SerologyTyping)
                .Select(m => new { HlaType = m.HlaTyping, GGroupCount = m.MatchingGGroups.Count() })
                .ToList();

            Assert.IsTrue(gGroupCounts.All(g => g.GGroupCount > 0));
        }

        [Test]
        public void BroadWhereSplitHasAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "9"));
        }

        [Test]
        public void BroadWhereSplitHasNoAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.Dqb1, "1"));
        }

        [Test]
        public void BroadHasSplitAndAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "21"));
        }

        [Test]
        public void SplitHasAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "51"));
        }

        [Test]
        public void SplitNoAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.C, "10"));
        }

        [Test]
        public void AssociatedWithSplitGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "3902"));
        }

        [Test]
        public void AssociatedWithBroadGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "4005"));
        }

        [Test]
        public void AssociatedWithNotSplitGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.Drb1, "103"));
        }

        [Test]
        public void NotSplitWithAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "2"));
        }

        [Test]
        public void NotSplitNoAssociatedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "1"));
        }

        [Test]
        public void DeletedGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.C, "11"));
        }

        [Test]
        public void B15BroadGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "15"));
        }

        [Test]
        public void B15SplitGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "62"));
        }

        [Test]
        public void B70BroadGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "70"));
        }

        [Test]
        public void B70SplitGGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "72"));
        }

        [Test]
        public void B15And70ShareMatchingGGroups()
        {
            var b15 = GetSingleMatchingTyping(MatchLocus.B, "15").MatchingGGroups;
            var b70 = GetSingleMatchingTyping(MatchLocus.B, "70").MatchingGGroups;

            Approvals.Verify(string.Join("\r\n", b15.Intersect(b70).OrderBy(p => p)));
        }

        private string GetGGroupsAsString(MatchLocus matchLocus, string serologyName)
        {
            return string.Join("\r\n",
                GetSingleMatchingTyping(matchLocus, serologyName)
                .MatchingGGroups
                .OrderBy(p => p));
        }
    }
}
