using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddDebugInfoToDonorImportHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailureCount",
                table: "DonorImportHistory",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportBegin",
                table: "DonorImportHistory",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportEnd",
                table: "DonorImportHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureCount",
                table: "DonorImportHistory");

            migrationBuilder.DropColumn(
                name: "ImportBegin",
                table: "DonorImportHistory");

            migrationBuilder.DropColumn(
                name: "ImportEnd",
                table: "DonorImportHistory");
        }
    }
}
