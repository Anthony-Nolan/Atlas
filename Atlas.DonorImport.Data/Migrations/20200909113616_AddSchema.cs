using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Donors");

            migrationBuilder.RenameTable(
                name: "Donors",
                newName: "Donors",
                newSchema: "Donors");

            migrationBuilder.RenameTable(
                name: "DonorLogs",
                newName: "DonorLogs",
                newSchema: "Donors");

            migrationBuilder.RenameTable(
                name: "DonorImportHistory",
                newName: "DonorImportHistory",
                newSchema: "Donors");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Donors",
                schema: "Donors",
                newName: "Donors");

            migrationBuilder.RenameTable(
                name: "DonorLogs",
                schema: "Donors",
                newName: "DonorLogs");

            migrationBuilder.RenameTable(
                name: "DonorImportHistory",
                schema: "Donors",
                newName: "DonorImportHistory");
        }
    }
}
