using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class CreateVerificationTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDateTime = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Comments = table.Column<string>(nullable: true),
                    TestHarness_Id = table.Column<int>(nullable: false),
                    SearchLociCount = table.Column<int>(nullable: false),
                    SearchRequest = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationRuns_TestHarnesses_TestHarness_Id",
                        column: x => x.TestHarness_Id,
                        principalTable: "TestHarnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchRequests",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerificationRun_Id = table.Column<int>(nullable: false),
                    PatientSimulant_Id = table.Column<int>(nullable: false),
                    AtlasSearchIdentifier = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchRequests_Simulants_PatientSimulant_Id",
                        column: x => x.PatientSimulant_Id,
                        principalTable: "Simulants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchRequests_VerificationRuns_VerificationRun_Id",
                        column: x => x.VerificationRun_Id,
                        principalTable: "VerificationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatchedDonors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestRecord_Id = table.Column<int>(nullable: false),
                    MatchedDonorSimulant_Id = table.Column<int>(nullable: false),
                    TotalMatchCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedDonors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchedDonors_Simulants_MatchedDonorSimulant_Id",
                        column: x => x.MatchedDonorSimulant_Id,
                        principalTable: "Simulants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchedDonors_SearchRequests_SearchRequestRecord_Id",
                        column: x => x.SearchRequestRecord_Id,
                        principalTable: "SearchRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatchProbabilities",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchedDonor_Id = table.Column<int>(nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    MismatchCount = table.Column<int>(nullable: false),
                    Probability = table.Column<decimal>(nullable: false)
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
                name: "IX_MatchedDonors_MatchedDonorSimulant_Id",
                table: "MatchedDonors",
                column: "MatchedDonorSimulant_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id",
                table: "MatchedDonors",
                column: "SearchRequestRecord_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchProbabilities_MatchedDonor_Id",
                table: "MatchProbabilities",
                column: "MatchedDonor_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_PatientSimulant_Id",
                table: "SearchRequests",
                column: "PatientSimulant_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_VerificationRun_Id",
                table: "SearchRequests",
                column: "VerificationRun_Id");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRuns_TestHarness_Id",
                table: "VerificationRuns",
                column: "TestHarness_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchProbabilities");

            migrationBuilder.DropTable(
                name: "MatchedDonors");

            migrationBuilder.DropTable(
                name: "SearchRequests");

            migrationBuilder.DropTable(
                name: "VerificationRuns");
        }
    }
}
