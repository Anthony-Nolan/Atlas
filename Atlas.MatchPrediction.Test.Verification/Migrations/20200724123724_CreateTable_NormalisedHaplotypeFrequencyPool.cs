using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Migrations
{
    public partial class CreateTable_NormalisedHaplotypeFrequencyPool : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NormalisedHaplotypeFrequencyPool",
                columns: table => new
                {
                    A = table.Column<string>(maxLength: 64, nullable: false),
                    B = table.Column<string>(maxLength: 64, nullable: false),
                    C = table.Column<string>(maxLength: 64, nullable: false),
                    DQB1 = table.Column<string>(maxLength: 64, nullable: false),
                    DRB1 = table.Column<string>(maxLength: 64, nullable: false),
                    Frequency = table.Column<decimal>(type: "decimal(20,20)", nullable: false),
                    CopyNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NormalisedHaplotypeFrequencyPool");
        }
    }
}
