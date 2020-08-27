using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyVerificationTables_MoveSearchResultsRetrievedColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchResultsRetrieved",
                table: "VerificationRuns");

            migrationBuilder.AddColumn<int>(
                name: "MatchedDonorCount",
                table: "SearchRequests",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SearchResultsRetrieved",
                table: "SearchRequests",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedDonorCount",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "SearchResultsRetrieved",
                table: "SearchRequests");

            migrationBuilder.AddColumn<bool>(
                name: "SearchResultsRetrieved",
                table: "VerificationRuns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
