using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.SearchAlgorithm.Data.Persistent.Migrations
{
    public partial class AddWmdaDatabaseVersionToDataRefreshHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WmdaDatabaseVersion",
                table: "DataRefreshHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WmdaDatabaseVersion",
                table: "DataRefreshHistory");
        }
    }
}
