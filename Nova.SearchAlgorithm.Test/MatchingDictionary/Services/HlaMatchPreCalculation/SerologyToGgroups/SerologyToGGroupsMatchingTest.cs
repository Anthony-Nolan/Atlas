using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyToGgroups
{
    [UseReporter(typeof(NUnitReporter))]
    [ApprovalTests.Namers.UseApprovalSubdirectory("../../../../Resources/MDPreCalc")]
    public class SerologyToGGroupsMatchingTest : MatchedOnTestBase<MatchedSerology>
    {
        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "9"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasNoAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.Dqb1, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadHasSplitAndAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "21"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitHasAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "51"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitNoAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.C, "10"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithSplit_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "3902"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithBroad_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "4005"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithNotSplit_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.Drb1, "103"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitWithAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "2"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitNoAssociated_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.A, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_Deleted_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.C, "11"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Broad_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "15"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Split_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "62"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Broad_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "70"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Split_GGroupsAreCorrect()
        {
            Approvals.Verify(GetGGroupsAsString(MatchLocus.B, "72"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15And70_ShareMatchingGGroups()
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
