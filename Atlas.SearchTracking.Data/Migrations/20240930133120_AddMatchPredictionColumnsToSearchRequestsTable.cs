using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AddMatchPredictionColumnsToSearchRequestsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MatchPrediction_FailureInfo_Json",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchPrediction_FailureInfo_Message");

            migrationBuilder.AddColumn<string>(
                name: "MatchPrediction_FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchPrediction_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "nvarchar(50)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchPrediction_FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "MatchPrediction_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.RenameColumn(
                name: "MatchPrediction_FailureInfo_Message",
                schema: "SearchTracking",
                table: "SearchRequests",
                newName: "MatchPrediction_FailureInfo_Json");
        }
    }
}
