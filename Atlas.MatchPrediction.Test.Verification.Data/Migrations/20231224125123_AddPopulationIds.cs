using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class AddPopulationIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DonorHfSetPopulationId",
                table: "MatchedDonors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientHfSetPopulationId",
                table: "MatchedDonors",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorHfSetPopulationId",
                table: "MatchedDonors");

            migrationBuilder.DropColumn(
                name: "PatientHfSetPopulationId",
                table: "MatchedDonors");
        }
    }
}
