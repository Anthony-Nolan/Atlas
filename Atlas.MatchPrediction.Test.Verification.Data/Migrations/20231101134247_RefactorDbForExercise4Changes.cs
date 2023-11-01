using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RefactorDbForExercise4Changes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestDonorExportRecords_TestHarnesses_TestHarness_Id",
                table: "TestDonorExportRecords");

            migrationBuilder.DropIndex(
                name: "IX_TestDonorExportRecords_TestHarness_Id",
                table: "TestDonorExportRecords");

            migrationBuilder.DropColumn(
                name: "TestHarness_Id",
                table: "TestDonorExportRecords");

            migrationBuilder.AddColumn<int>(
                name: "ExportRecord_Id",
                table: "TestHarnesses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Started",
                table: "TestDonorExportRecords",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_TestHarnesses_ExportRecord_Id",
                table: "TestHarnesses",
                column: "ExportRecord_Id",
                unique: true,
                filter: "[ExportRecord_Id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TestHarnesses_TestDonorExportRecords_ExportRecord_Id",
                table: "TestHarnesses",
                column: "ExportRecord_Id",
                principalTable: "TestDonorExportRecords",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestHarnesses_TestDonorExportRecords_ExportRecord_Id",
                table: "TestHarnesses");

            migrationBuilder.DropIndex(
                name: "IX_TestHarnesses_ExportRecord_Id",
                table: "TestHarnesses");

            migrationBuilder.DropColumn(
                name: "ExportRecord_Id",
                table: "TestHarnesses");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Started",
                table: "TestDonorExportRecords",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<int>(
                name: "TestHarness_Id",
                table: "TestDonorExportRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TestDonorExportRecords_TestHarness_Id",
                table: "TestDonorExportRecords",
                column: "TestHarness_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestDonorExportRecords_TestHarnesses_TestHarness_Id",
                table: "TestDonorExportRecords",
                column: "TestHarness_Id",
                principalTable: "TestHarnesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
