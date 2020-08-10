using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class DonorImportLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorLogs",
                columns: table => new
                {
                    ExternalDonorId = table.Column<string>(nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorLogs", x => x.ExternalDonorId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorLogs_ExternalDonorId",
                table: "DonorLogs",
                column: "ExternalDonorId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorLogs");
        }
    }
}
