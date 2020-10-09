using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class AddIndexesToOptimiseQueryingOfVerificationResults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_VerificationRun_Id",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_MatchCounts_MatchedDonor_Id",
                table: "MatchCounts");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_AtlasSearchIdentifier",
                table: "SearchRequests",
                column: "AtlasSearchIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_VerificationRun_Id_PatientSimulant_Id_SearchResultsRetrieved",
                table: "SearchRequests",
                columns: new[] { "VerificationRun_Id", "PatientSimulant_Id", "SearchResultsRetrieved" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_MatchedDonorSimulant_Id_TotalMatchCount",
                table: "MatchedDonors",
                columns: new[] { "SearchRequestRecord_Id", "MatchedDonorSimulant_Id", "TotalMatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchCounts_MatchedDonor_Id_Locus_MatchCount",
                table: "MatchCounts",
                columns: new[] { "MatchedDonor_Id", "Locus", "MatchCount" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_AtlasSearchIdentifier",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_VerificationRun_Id_PatientSimulant_Id_SearchResultsRetrieved",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_MatchedDonorSimulant_Id_TotalMatchCount",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_MatchCounts_MatchedDonor_Id_Locus_MatchCount",
                table: "MatchCounts");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_VerificationRun_Id",
                table: "SearchRequests",
                column: "VerificationRun_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id",
                table: "MatchedDonors",
                column: "SearchRequestRecord_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchCounts_MatchedDonor_Id",
                table: "MatchCounts",
                column: "MatchedDonor_Id");
        }
    }
}
