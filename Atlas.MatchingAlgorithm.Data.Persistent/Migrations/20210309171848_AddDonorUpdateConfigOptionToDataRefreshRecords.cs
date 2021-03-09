using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddDonorUpdateConfigOptionToDataRefreshRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShouldMarkAllDonorsAsUpdated",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShouldMarkAllDonorsAsUpdated",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
