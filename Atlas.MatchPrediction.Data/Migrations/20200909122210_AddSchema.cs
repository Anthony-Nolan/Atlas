using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MatchPrediction");

            migrationBuilder.RenameTable(
                name: "HaplotypeFrequencySets",
                newName: "HaplotypeFrequencySets",
                newSchema: "MatchPrediction");

            migrationBuilder.RenameTable(
                name: "HaplotypeFrequencies",
                newName: "HaplotypeFrequencies",
                newSchema: "MatchPrediction");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "HaplotypeFrequencySets",
                schema: "MatchPrediction",
                newName: "HaplotypeFrequencySets");

            migrationBuilder.RenameTable(
                name: "HaplotypeFrequencies",
                schema: "MatchPrediction",
                newName: "HaplotypeFrequencies");
        }
    }
}
