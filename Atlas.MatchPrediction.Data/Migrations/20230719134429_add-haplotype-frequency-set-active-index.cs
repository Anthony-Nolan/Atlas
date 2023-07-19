using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class addhaplotypefrequencysetactiveindex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencySets_Active",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencySets",
                column: "Active");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencySets_Active",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencySets");
        }
    }
}
