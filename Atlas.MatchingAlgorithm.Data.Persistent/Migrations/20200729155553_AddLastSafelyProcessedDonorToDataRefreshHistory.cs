using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddLastSafelyProcessedDonorToDataRefreshHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDonorWithProcessedHla",
                table: "DataRefreshHistory");

            migrationBuilder.AddColumn<int>(
                name: "LastSafelyProcessedDonor",
                table: "DataRefreshHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSafelyProcessedDonor",
                table: "DataRefreshHistory");

            migrationBuilder.AddColumn<int>(
                name: "LastDonorWithProcessedHla",
                table: "DataRefreshHistory",
                type: "int",
                nullable: true);
        }
    }
}
