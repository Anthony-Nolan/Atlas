using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class AddDonorExportData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonorType",
                table: "SubjectInfo",
                type: "nvarchar(10)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TestDonorExportRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Started = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Exported = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataRefreshCompleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataRefreshRecordId = table.Column<int>(type: "int", nullable: true),
                    WasDataRefreshSuccessful = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestDonorExportRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestDonorExportRecords");

            migrationBuilder.DropColumn(
                name: "DonorType",
                table: "SubjectInfo");
        }
    }
}
