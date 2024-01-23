using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class AddSearchTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestDonorExportRecord_Id = table.Column<int>(type: "int", nullable: false),
                    SearchRequestsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchSets_TestDonorExportRecords_TestDonorExportRecord_Id",
                        column: x => x.TestDonorExportRecord_Id,
                        principalTable: "TestDonorExportRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchSet_Id = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DonorMismatchCount = table.Column<int>(type: "int", nullable: false),
                    AtlasSearchIdentifier = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    SearchResultsRetrieved = table.Column<bool>(type: "bit", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    MatchedDonorCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchRequests_SearchSets_SearchSet_Id",
                        column: x => x.SearchSet_Id,
                        principalTable: "SearchSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchRequests_SubjectInfo_PatientId",
                        column: x => x.PatientId,
                        principalTable: "SubjectInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchedDonors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestRecord_Id = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    TotalMatchCount = table.Column<int>(type: "int", nullable: false),
                    TypedLociCount = table.Column<int>(type: "int", nullable: false),
                    WasPatientRepresented = table.Column<bool>(type: "bit", nullable: true),
                    WasDonorRepresented = table.Column<bool>(type: "bit", nullable: true),
                    MatchingResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchPredictionResult = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedDonors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchedDonors_SearchRequests_SearchRequestRecord_Id",
                        column: x => x.SearchRequestRecord_Id,
                        principalTable: "SearchRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchedDonors_SubjectInfo_DonorId",
                        column: x => x.DonorId,
                        principalTable: "SubjectInfo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatchCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchedDonor_Id = table.Column<int>(type: "int", nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    MatchCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchCounts_MatchedDonors_MatchedDonor_Id",
                        column: x => x.MatchedDonor_Id,
                        principalTable: "MatchedDonors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchProbabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchedDonor_Id = table.Column<int>(type: "int", nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(6,5)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchProbabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchProbabilities_MatchedDonors_MatchedDonor_Id",
                        column: x => x.MatchedDonor_Id,
                        principalTable: "MatchedDonors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchCounts_MatchedDonor_Id_Locus_MatchCount",
                table: "MatchCounts",
                columns: new[] { "MatchedDonor_Id", "Locus", "MatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_DonorId",
                table: "MatchedDonors",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorId_TotalMatchCount",
                table: "MatchedDonors",
                columns: new[] { "SearchRequestRecord_Id", "DonorId", "TotalMatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id_Locus_MismatchCount",
                table: "MatchProbabilities",
                columns: new[] { "MatchedDonor_Id", "Locus", "MismatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_AtlasSearchIdentifier",
                table: "SearchRequests",
                column: "AtlasSearchIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_PatientId",
                table: "SearchRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_SearchSet_Id",
                table: "SearchRequests",
                column: "SearchSet_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSets_TestDonorExportRecord_Id",
                table: "SearchSets",
                column: "TestDonorExportRecord_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchCounts");

            migrationBuilder.DropTable(
                name: "MatchProbabilities");

            migrationBuilder.DropTable(
                name: "MatchedDonors");

            migrationBuilder.DropTable(
                name: "SearchRequests");

            migrationBuilder.DropTable(
                name: "SearchSets");
        }
    }
}
