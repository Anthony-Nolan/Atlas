using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class AddProbabilityAsPercentage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProbabilityAsPercentage",
                table: "MatchProbabilities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProbabilityAsPercentage",
                table: "MatchPredictionResults",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProbabilityAsPercentage",
                table: "MatchProbabilities");

            migrationBuilder.DropColumn(
                name: "ProbabilityAsPercentage",
                table: "MatchPredictionResults");
        }
    }
}
