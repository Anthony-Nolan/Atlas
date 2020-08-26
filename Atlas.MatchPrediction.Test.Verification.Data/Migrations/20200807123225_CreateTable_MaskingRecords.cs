using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class CreateTable_MaskingRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaskingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestHarness_Id = table.Column<int>(nullable: false),
                    TestIndividualCategory = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    ProportionDeleted = table.Column<int>(nullable: false),
                    ProportionsMasked = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaskingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaskingRecords_TestHarnesses_TestHarness_Id",
                        column: x => x.TestHarness_Id,
                        principalTable: "TestHarnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaskingRecords_TestHarness_Id",
                table: "MaskingRecords",
                column: "TestHarness_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaskingRecords");
        }
    }
}
