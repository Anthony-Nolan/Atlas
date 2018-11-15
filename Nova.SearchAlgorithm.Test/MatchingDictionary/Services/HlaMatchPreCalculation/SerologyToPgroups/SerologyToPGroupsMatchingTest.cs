using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyToPgroups
{
    [UseReporter(typeof(NUnitReporter))]
    [ApprovalTests.Namers.UseApprovalSubdirectory("../../../../Resources/MDPreCalc")]
    public class SerologyToPGroupsMatchingTest : MatchedOnTestBase<MatchedSerology>
    {
        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "9"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.Dqb1, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadHasSplitAndAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "21"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitHasAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "51"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.C, "10"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithSplit_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "3902"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithBroad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "4005"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithNotSplit_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.Drb1, "103"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitWithAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "2"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.A, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_Deleted_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.C, "11"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Broad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "15"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Split_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "62"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Broad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "70"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Split_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(MatchLocus.B, "72"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15And70_ShareMatchingPGroups()
        {
            var b15 = GetSingleMatchingTyping(MatchLocus.B, "15").MatchingPGroups;
            var b70 = GetSingleMatchingTyping(MatchLocus.B, "70").MatchingPGroups;

            Approvals.Verify(string.Join("\r\n", b15.Intersect(b70).OrderBy(p => p)));
        }

        private string GetPGroupsAsString(MatchLocus matchLocus, string serologyName)
        {
            return string.Join("\r\n",
                GetSingleMatchingTyping(matchLocus, serologyName)
                .MatchingPGroups
                .OrderBy(p => p));
        }
    }
}
