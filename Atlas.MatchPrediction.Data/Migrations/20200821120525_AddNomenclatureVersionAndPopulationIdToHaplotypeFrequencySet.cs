using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddNomenclatureVersionAndPopulationIdToHaplotypeFrequencySet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HlaNomenclatureVersion",
                table: "HaplotypeFrequencySets",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PopulationId",
                table: "HaplotypeFrequencySets",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HlaNomenclatureVersion",
                table: "HaplotypeFrequencySets");

            migrationBuilder.DropColumn(
                name: "PopulationId",
                table: "HaplotypeFrequencySets");
        }
    }
}
