using System.Linq;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation.SerologyToPgroups
{
    [UseReporter(typeof(NUnitReporter))]
    [UseApprovalSubdirectory("Approvals")]
    internal class SerologyToPGroupsMatchingTest : MatchedOnTestBase<MatchedSerology>
    {
        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.A, "9"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadWhereSplitHasNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.Dqb1, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_BroadHasSplitAndAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "21"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitHasAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "51"));
        }

        [Test]
        public void SerologyToAlleleMatching_SplitNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.C, "10"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithSplit_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "3902"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithBroad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "4005"));
        }

        [Test]
        public void SerologyToAlleleMatching_AssociatedWithNotSplit_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.Drb1, "103"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitWithAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.A, "2"));
        }

        [Test]
        public void SerologyToAlleleMatching_NotSplitNoAssociated_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.A, "1"));
        }

        [Test]
        public void SerologyToAlleleMatching_Deleted_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.C, "11"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Broad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "15"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15Split_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "62"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Broad_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "70"));
        }

        [Test]
        public void SerologyToAlleleMatching_B70Split_PGroupsAreCorrect()
        {
            Approvals.Verify(GetPGroupsAsString(Locus.B, "72"));
        }

        [Test]
        public void SerologyToAlleleMatching_B15And70_ShareMatchingPGroups()
        {
            var b15 = GetSingleMatchingTyping(Locus.B, "15").MatchingPGroups;
            var b70 = GetSingleMatchingTyping(Locus.B, "70").MatchingPGroups;

            Approvals.Verify(b15.Intersect(b70).OrderBy(p => p).StringJoinWithNewline());
        }

        private string GetPGroupsAsString(Locus locus, string serologyName)
        {
            return GetSingleMatchingTyping(locus, serologyName)
                .MatchingPGroups
                .OrderBy(p => p)
                .StringJoinWithNewline();
        }
    }
}
