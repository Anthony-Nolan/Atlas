using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class SetPrecisionOfFrequencyColumnToSameValueAsScale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Frequency",
                table: "HaplotypeFrequencies",
                type: "decimal(20,20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(21,20)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Frequency",
                table: "HaplotypeFrequencies",
                type: "decimal(21,20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,20)");
        }
    }
}
