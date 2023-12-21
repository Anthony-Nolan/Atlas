using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RebuildIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id",
                table: "MatchProbabilities");

            migrationBuilder.CreateIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id_Locus_MismatchCount",
                table: "MatchProbabilities",
                columns: new[] { "MatchedDonor_Id", "Locus", "MismatchCount" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id_Locus_MismatchCount",
                table: "MatchProbabilities");

            migrationBuilder.CreateIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id",
                table: "MatchProbabilities",
                column: "MatchedDonor_Id");
        }
    }
}
