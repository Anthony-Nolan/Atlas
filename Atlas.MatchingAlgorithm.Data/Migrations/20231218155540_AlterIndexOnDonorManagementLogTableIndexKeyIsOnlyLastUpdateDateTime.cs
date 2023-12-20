using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AlterIndexOnDonorManagementLogTableIndexKeyIsOnlyLastUpdateDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonorManagementLogs_DonorId_LastUpdateDateTime",
                table: "DonorManagementLogs");

            migrationBuilder.CreateIndex(
                name: "IX_DonorManagementLogs_LastUpdateDateTime",
                table: "DonorManagementLogs",
                column: "LastUpdateDateTime")
                .Annotation("SqlServer:Include", new[] { "DonorId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonorManagementLogs_LastUpdateDateTime",
                table: "DonorManagementLogs");

            migrationBuilder.CreateIndex(
                name: "IX_DonorManagementLogs_DonorId_LastUpdateDateTime",
                table: "DonorManagementLogs",
                columns: new[] { "DonorId", "LastUpdateDateTime" });
        }
    }
}
