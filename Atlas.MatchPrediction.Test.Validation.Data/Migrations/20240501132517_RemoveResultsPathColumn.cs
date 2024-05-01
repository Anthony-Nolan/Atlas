using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class RemoveResultsPathColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultsPath",
                table: "HomeworkSets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultsPath",
                table: "HomeworkSets",
                type: "nvarchar(516)",
                nullable: false,
                defaultValue: "");
        }
    }
}
