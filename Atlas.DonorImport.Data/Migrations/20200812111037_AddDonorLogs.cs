using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddDonorLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorLogs",
                columns: table => new
                {
                    ExternalDonorCode = table.Column<string>(nullable: false),
                    LastUpdateFileUploadTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorLogs", x => x.ExternalDonorCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorLogs_ExternalDonorCode",
                table: "DonorLogs",
                column: "ExternalDonorCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorLogs");
        }
    }
}
