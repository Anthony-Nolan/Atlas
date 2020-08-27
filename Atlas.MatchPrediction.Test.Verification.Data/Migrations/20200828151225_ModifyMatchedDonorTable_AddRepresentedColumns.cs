using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyMatchedDonorTable_AddRepresentedColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "MatchProbabilities",
                type: "decimal(5,5)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<bool>(
                name: "WasDonorRepresented",
                table: "MatchedDonors",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WasPatientRepresented",
                table: "MatchedDonors",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasDonorRepresented",
                table: "MatchedDonors");

            migrationBuilder.DropColumn(
                name: "WasPatientRepresented",
                table: "MatchedDonors");

            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "MatchProbabilities",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,5)",
                oldNullable: true);
        }
    }
}
