using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AmendSearchTrackingSearchRequestsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResultsSentTimeUTC",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "ResultsSentTimeUtc");

            migrationBuilder.RenameColumn(
                name: "RequestTimeUTC",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "RequestTimeUtc");

            migrationBuilder.RenameColumn(
                name: "MatchingAlgorithm_ResultsSentTimeUTC",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchingAlgorithm_ResultsSentTimeUtc");

            migrationBuilder.RenameColumn(
                name: "MatchingAlgorithm_FailureInfo_Json",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchingAlgorithm_FailureInfo_Message");

            migrationBuilder.AddColumn<string>(
                name: "MatchingAlgorithm_FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchingAlgorithm_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchingAlgorithm_FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "MatchingAlgorithm_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.RenameColumn(
                name: "ResultsSentTimeUtc",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "ResultsSentTimeUTC");

            migrationBuilder.RenameColumn(
                name: "RequestTimeUtc",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "RequestTimeUTC");

            migrationBuilder.RenameColumn(
                name: "MatchingAlgorithm_ResultsSentTimeUtc",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchingAlgorithm_ResultsSentTimeUTC");

            migrationBuilder.RenameColumn(
                name: "MatchingAlgorithm_FailureInfo_Message",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchingAlgorithm_FailureInfo_Json");
        }
    }
}
