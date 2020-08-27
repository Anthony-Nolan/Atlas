using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyVerificationRunsTable_AddColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SearchRequestsSubmitted",
                table: "VerificationRuns",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SearchResultsRetrieved",
                table: "VerificationRuns",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchRequestsSubmitted",
                table: "VerificationRuns");

            migrationBuilder.DropColumn(
                name: "SearchResultsRetrieved",
                table: "VerificationRuns");
        }
    }
}
