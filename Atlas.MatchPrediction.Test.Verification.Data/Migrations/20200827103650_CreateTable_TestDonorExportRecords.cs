using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class CreateTable_TestDonorExportRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestDonorExportRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestHarness_Id = table.Column<int>(nullable: false),
                    Started = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Exported = table.Column<DateTimeOffset>(nullable: true),
                    DataRefreshCompleted = table.Column<DateTimeOffset>(nullable: true),
                    DataRefreshRecordId = table.Column<int>(nullable: true),
                    WasDataRefreshSuccessful = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestDonorExportRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestDonorExportRecords_TestHarnesses_TestHarness_Id",
                        column: x => x.TestHarness_Id,
                        principalTable: "TestHarnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestDonorExportRecords_TestHarness_Id",
                table: "TestDonorExportRecords",
                column: "TestHarness_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestDonorExportRecords");
        }
    }
}
