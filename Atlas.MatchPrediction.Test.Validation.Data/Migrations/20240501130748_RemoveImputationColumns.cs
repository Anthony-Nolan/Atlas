using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class RemoveImputationColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorImputationCompleted",
                table: "PatientDonorPairs");

            migrationBuilder.DropColumn(
                name: "PatientImputationCompleted",
                table: "PatientDonorPairs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DonorImputationCompleted",
                table: "PatientDonorPairs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PatientImputationCompleted",
                table: "PatientDonorPairs",
                type: "bit",
                nullable: true);
        }
    }
}
