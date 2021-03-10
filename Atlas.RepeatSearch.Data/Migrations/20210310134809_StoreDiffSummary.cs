using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.RepeatSearch.Data.Migrations
{
    public partial class StoreDiffSummary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RemovedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords");

            migrationBuilder.DropColumn(
                name: "RemovedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedResultCount",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords");
        }
    }
}
