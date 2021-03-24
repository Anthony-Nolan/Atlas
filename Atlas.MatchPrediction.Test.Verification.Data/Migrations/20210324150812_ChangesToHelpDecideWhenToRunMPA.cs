using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ChangesToHelpDecideWhenToRunMPA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasMatchPredictionRun",
                table: "SearchRequests",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TypingCategory",
                table: "NormalisedPool",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasMatchPredictionRun",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "TypingCategory",
                table: "NormalisedPool");
        }
    }
}
