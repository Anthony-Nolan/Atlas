using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.SearchAlgorithm.Data.Persistent.Migrations
{
    public partial class AddSuccessFlagToDataRefreshHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasSuccessful",
                table: "DataRefreshHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasSuccessful",
                table: "DataRefreshHistory");
        }
    }
}
