using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class CreateTable_MatchCounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchCounts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchedDonor_Id = table.Column<int>(nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    MatchCount = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchCounts_MatchedDonors_MatchedDonor_Id",
                        column: x => x.MatchedDonor_Id,
                        principalTable: "MatchedDonors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchCounts_MatchedDonor_Id",
                table: "MatchCounts",
                column: "MatchedDonor_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchCounts");
        }
    }
}
