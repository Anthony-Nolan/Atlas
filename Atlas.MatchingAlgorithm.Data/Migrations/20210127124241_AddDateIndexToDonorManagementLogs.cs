using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddDateIndexToDonorManagementLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DonorManagementLogs_DonorId_LastUpdateDateTime",
                table: "DonorManagementLogs",
                columns: new[] { "DonorId", "LastUpdateDateTime" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonorManagementLogs_DonorId_LastUpdateDateTime",
                table: "DonorManagementLogs");
        }
    }
}
