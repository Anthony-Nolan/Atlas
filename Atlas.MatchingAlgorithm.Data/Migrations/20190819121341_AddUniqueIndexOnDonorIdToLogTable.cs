using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddUniqueIndexOnDonorIdToLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DonorManagementLogs_DonorId",
                table: "DonorManagementLogs",
                column: "DonorId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonorManagementLogs_DonorId",
                table: "DonorManagementLogs");
        }
    }
}
