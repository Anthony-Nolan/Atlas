using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyMatchProbabilitiesTable_IncreasePrecisionOfProbabilityColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "MatchProbabilities",
                type: "decimal(6,5)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,5)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "MatchProbabilities",
                type: "decimal(5,5)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,5)",
                oldNullable: true);
        }
    }
}
