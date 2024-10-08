using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AddAdditionalPerformanceTrackingColumnsToSearchRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AreBetterMatchesIncluded",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DonorRegistryCodes",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMatchPredictionRun",
                schema: "SearchTracking",
                table: "SearchRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreBetterMatchesIncluded",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "DonorRegistryCodes",
                schema: "SearchTracking",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "IsMatchPredictionRun",
                schema: "SearchTracking",
                table: "SearchRequests");
        }
    }
}
