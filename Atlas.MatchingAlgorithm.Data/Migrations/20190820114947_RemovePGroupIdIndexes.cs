using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class RemovePGroupIdIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtA");

            migrationBuilder.DropIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtB");

            migrationBuilder.DropIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtC");

            migrationBuilder.DropIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDRB1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtA",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtB",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtC",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDQB1",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDRB1",
                column: "PGroup_Id");
        }
    }
}
