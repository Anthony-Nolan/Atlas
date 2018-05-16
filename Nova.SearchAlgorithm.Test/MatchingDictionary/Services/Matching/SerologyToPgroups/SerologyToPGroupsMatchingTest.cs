using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.SerologyToPgroups
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedSerology))]
    [UseReporter(typeof(NUnitReporter))]
    public class SerologyToPGroupsMatchingTest : MatchedOnTestBase<MatchedSerology>
    {
        public SerologyToPGroupsMatchingTest(IEnumerable<MatchedSerology> matchingTypes) : base(matchingTypes)
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
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "9"));
        }

        [Test]
        public void BroadWhereSplitHasNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.Dqb1, "1"));
        }

        [Test]
        public void BroadHasSplitAndAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "21"));
        }

        [Test]
        public void SplitHasAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "51"));
        }

        [Test]
        public void SplitNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.C, "10"));
        }

        [Test]
        public void AssociatedWithSplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "3902"));
        }

        [Test]
        public void AssociatedWithBroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "4005"));
        }

        [Test]
        public void AssociatedWithNotSplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.Drb1, "103"));
        }

        [Test]
        public void NotSplitWithAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "2"));
        }

        [Test]
        public void NotSplitNoAssociatedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "1"));
        }

        [Test]
        public void DeletedPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.C, "11"));
        }

        [Test]
        public void B15BroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "15"));
        }

        [Test]
        public void B15SplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "62"));
        }

        [Test]
        public void B70BroadPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "70"));
        }

        [Test]
        public void B70SplitPGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "72"));
        }

        [Test]
        public void B15And70ShareMatchingPGroups()
        {
            var b15 = GetSingleMatchingType(MatchLocus.B, "15").MatchingPGroups;
            var b70 = GetSingleMatchingType(MatchLocus.B, "70").MatchingPGroups;

            Approvals.Verify(string.Join("\r\n", b15.Intersect(b70).OrderBy(p => p)));
        }

        private string GetPGroupsAsString(MatchLocus matchLocus, string serologyName)
        {
            return string.Join("\r\n",
                GetSingleMatchingType(matchLocus, serologyName)
                .MatchingPGroups
                .OrderBy(p => p));
        }
    }
}
