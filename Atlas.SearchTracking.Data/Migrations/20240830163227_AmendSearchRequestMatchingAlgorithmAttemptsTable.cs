using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AmendSearchRequestMatchingAlgorithmAttemptsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchRequestMatchingAlgorithmAttemptTimings",
                schema: "SearchTracking");

            migrationBuilder.CreateTable(
                name: "SearchRequestMatchingAlgorithmAttempts",
                schema: "SearchTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    InitiationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlgorithmCore_Matching_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Matching_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Scoring_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Scoring_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersistingResults_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersistingResults_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    FailureInfo_Type = table.Column<int>(type: "int", nullable: true),
                    FailureInfo_Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureInfo_ExceptionStacktrace = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequestMatchingAlgorithmAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchRequestMatchingAlgorithmAttempts_SearchRequests_SearchRequestId",
                        column: x => x.SearchRequestId,
                        principalSchema: "SearchTracking",
                        principalTable: "SearchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttempts",
                columns: new[] { "SearchRequestId", "AttemptNumber" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchRequestMatchingAlgorithmAttempts",
                schema: "SearchTracking");

            migrationBuilder.CreateTable(
                name: "SearchRequestMatchingAlgorithmAttemptTimings",
                schema: "SearchTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestId = table.Column<int>(type: "int", nullable: false),
                    AlgorithmCore_Matching_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Matching_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Scoring_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_Scoring_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    CompletionTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InitiationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersistingResults_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersistingResults_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequestMatchingAlgorithmAttemptTimings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchRequestMatchingAlgorithmAttemptTimings_SearchRequests_SearchRequestId",
                        column: x => x.SearchRequestId,
                        principalSchema: "SearchTracking",
                        principalTable: "SearchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttemptTimings",
                columns: new[] { "SearchRequestId", "AttemptNumber" });
        }
    }
}
