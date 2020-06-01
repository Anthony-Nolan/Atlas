using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class RenameHlaVersionColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WmdaDatabaseVersion",
                table: "DataRefreshHistory",
                newName: "HlaNomenclatureVersion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HlaNomenclatureVersion",
                table: "DataRefreshHistory",
                newName: "WmdaDatabaseVersion");
        }
    }
}
