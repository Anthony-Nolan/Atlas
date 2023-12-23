using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class NewLocusScoreColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchConfidence_1",
                table: "LocusMatchDetails",
                type: "nvarchar(32)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchConfidence_2",
                table: "LocusMatchDetails",
                type: "nvarchar(32)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchGrade_1",
                table: "LocusMatchDetails",
                type: "nvarchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchGrade_2",
                table: "LocusMatchDetails",
                type: "nvarchar(128)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchConfidence_1",
                table: "LocusMatchDetails");

            migrationBuilder.DropColumn(
                name: "MatchConfidence_2",
                table: "LocusMatchDetails");

            migrationBuilder.DropColumn(
                name: "MatchGrade_1",
                table: "LocusMatchDetails");

            migrationBuilder.DropColumn(
                name: "MatchGrade_2",
                table: "LocusMatchDetails");
        }
    }
}
