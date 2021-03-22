using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class AlterMatchedDonorsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SearchResult",
                table: "MatchedDonors",
                newName: "MatchingResult");

            migrationBuilder.AlterColumn<bool>(
                name: "WasPatientRepresented",
                table: "MatchedDonors",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "WasDonorRepresented",
                table: "MatchedDonors",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "MatchPredictionResult",
                table: "MatchedDonors",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchPredictionResult",
                table: "MatchedDonors");

            migrationBuilder.RenameColumn(
                name: "MatchingResult",
                table: "MatchedDonors",
                newName: "SearchResult");

            migrationBuilder.AlterColumn<bool>(
                name: "WasPatientRepresented",
                table: "MatchedDonors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "WasDonorRepresented",
                table: "MatchedDonors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }
    }
}
