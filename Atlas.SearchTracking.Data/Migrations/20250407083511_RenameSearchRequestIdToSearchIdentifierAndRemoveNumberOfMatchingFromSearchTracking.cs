using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class RenameSearchRequestIdToSearchIdentifierAndRemoveNumberOfMatchingFromSearchTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchingAlgorithm_NumberOfMatching",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.RenameColumn(
                name: "SearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "SearchIdentifier");

            migrationBuilder.RenameColumn(
                name: "OriginalSearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "OriginalSearchIdentifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SearchIdentifier",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "SearchRequestId");

            migrationBuilder.RenameColumn(
                name: "OriginalSearchIdentifier",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "OriginalSearchRequestId");

            migrationBuilder.AddColumn<int>(
                name: "MatchingAlgorithm_NumberOfMatching",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "int",
                nullable: true);
        }
    }
}
