using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddTypingCategoryToHaplotypeFrequencies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TypingCategory",
                table: "HaplotypeFrequencies",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypingCategory",
                table: "HaplotypeFrequencies");
        }
    }
}
