using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class AddHomeworkRequestTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeworkSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SetName = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ResultsPath = table.Column<string>(type: "nvarchar(516)", nullable: false),
                    MatchLoci = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    SubmittedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeworkSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientDonorPairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    DonorId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DidPatientHaveMissingHla = table.Column<bool>(type: "bit", nullable: true),
                    DidDonorHaveMissingHla = table.Column<bool>(type: "bit", nullable: true),
                    PatientImputationCompleted = table.Column<bool>(type: "bit", nullable: true),
                    DonorImputationCompleted = table.Column<bool>(type: "bit", nullable: true),
                    MatchingGenotypesCalculated = table.Column<bool>(type: "bit", nullable: true),
                    HomeworkSet_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDonorPairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDonorPairs_HomeworkSets_HomeworkSet_Id",
                        column: x => x.HomeworkSet_Id,
                        principalTable: "HomeworkSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkSets_SetName",
                table: "HomeworkSets",
                column: "SetName");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDonorPairs_DonorId_PatientId_HomeworkSet_Id_IsProcessed",
                table: "PatientDonorPairs",
                columns: new[] { "DonorId", "PatientId", "HomeworkSet_Id", "IsProcessed" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientDonorPairs_HomeworkSet_Id",
                table: "PatientDonorPairs",
                column: "HomeworkSet_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientDonorPairs");

            migrationBuilder.DropTable(
                name: "HomeworkSets");
        }
    }
}
