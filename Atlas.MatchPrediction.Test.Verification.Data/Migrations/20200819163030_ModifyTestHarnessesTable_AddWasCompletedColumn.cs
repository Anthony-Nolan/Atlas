using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyTestHarnessesTable_AddWasCompletedColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasCompleted",
                table: "TestHarnesses",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasCompleted",
                table: "TestHarnesses");
        }
    }
}
