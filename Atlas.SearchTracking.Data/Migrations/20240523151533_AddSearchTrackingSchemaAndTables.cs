using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AddSearchTrackingSchemaAndTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SearchTracking");

            migrationBuilder.CreateTable(
                name: "SearchRequests",
                schema: "SearchTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRepeatSearch = table.Column<bool>(type: "bit", nullable: false),
                    OriginalSearchRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RepeatSearchCutOffDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchCriteria = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DonorType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestTimeUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchingAlgorithm_IsSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    MatchingAlgorithm_FailureInfo_Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchingAlgorithm_TotalAttemptsNumber = table.Column<byte>(type: "tinyint", nullable: true),
                    MatchingAlgorithm_NumberOfMatching = table.Column<int>(type: "int", nullable: true),
                    MatchingAlgorithm_NumberOfResults = table.Column<int>(type: "int", nullable: true),
                    RepeatSearch_AddedResultCount = table.Column<int>(type: "int", nullable: true),
                    RepeatSearch_RemovedResultCount = table.Column<int>(type: "int", nullable: true),
                    RepeatSearch_UpdatedResultCount = table.Column<int>(type: "int", nullable: true),
                    MatchingAlgorithm_HlaNomenclatureVersion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MatchingAlgorithm_ResultsSent = table.Column<bool>(type: "bit", nullable: true),
                    MatchingAlgorithm_ResultsSentTimeUTC = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchPrediction_IsSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    MatchPrediction_FailureInfo_Json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchPrediction_DonorsPerBatch = table.Column<int>(type: "int", nullable: true),
                    MatchPrediction_TotalNumberOfBatches = table.Column<int>(type: "int", nullable: true),
                    ResultsSent = table.Column<bool>(type: "bit", nullable: true),
                    ResultsSentTimeUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchRequestMatchingAlgorithmAttemptTimings",
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
                    CompletionTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "SearchRequestMatchPredictionTimings",
                schema: "SearchTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchRequestId = table.Column<int>(type: "int", nullable: false),
                    InitiationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrepareBatches_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrepareBatches_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_RunningBatches_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlgorithmCore_RunningBatches_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersistingResults_StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersistingResults_EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequestMatchPredictionTimings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchRequestMatchPredictionTimings_SearchRequests_SearchRequestId",
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
                columns: new[] { "SearchRequestId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestMatchPredictionTimings_SearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                column: "SearchRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequests",
                column: "SearchRequestId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchRequestMatchingAlgorithmAttemptTimings",
                schema: "SearchTracking");

            migrationBuilder.DropTable(
                name: "SearchRequestMatchPredictionTimings",
                schema: "SearchTracking");

            migrationBuilder.DropTable(
                name: "SearchRequests",
                schema: "SearchTracking");
        }
    }
}
