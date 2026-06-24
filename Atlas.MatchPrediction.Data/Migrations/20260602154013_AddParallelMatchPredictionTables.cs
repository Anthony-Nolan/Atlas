using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParallelMatchPredictionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParallelMatchPredictionRuns",
                schema: "MatchPrediction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchIdentifier = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRepeatSearch = table.Column<bool>(type: "bit", nullable: false),
                    RepeatSearchIdentifier = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultsFileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResultsBatched = table.Column<bool>(type: "bit", nullable: false),
                    BatchFolderName = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    MatchingAlgorithmElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                    SearchInitiatedTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalBatchCount = table.Column<int>(type: "int", nullable: false),
                    MatchPredictionRunInitiatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StatusDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalisationLeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinalisedTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParallelMatchPredictionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParallelMatchPredictionBatches",
                schema: "MatchPrediction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    BatchSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    BatchStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Requested"),
                    ResultReceivedTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultLocationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    FailureException = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParallelMatchPredictionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParallelMatchPredictionBatches_ParallelMatchPredictionRuns_RunId",
                        column: x => x.RunId,
                        principalSchema: "MatchPrediction",
                        principalTable: "ParallelMatchPredictionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParallelMatchPredictionBatches_RunId_BatchSequenceNumber",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                columns: new[] { "RunId", "BatchSequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParallelMatchPredictionRuns_Status_FinalisedTimeUtc",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns",
                columns: new[] { "Status", "FinalisedTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParallelMatchPredictionBatches",
                schema: "MatchPrediction");

            migrationBuilder.DropTable(
                name: "ParallelMatchPredictionRuns",
                schema: "MatchPrediction");
        }
    }
}
