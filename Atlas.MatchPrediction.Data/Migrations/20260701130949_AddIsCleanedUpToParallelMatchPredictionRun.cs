using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCleanedUpToParallelMatchPredictionRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParallelMatchPredictionRuns_Status_FinalisedTimeUtc",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns");

            migrationBuilder.AddColumn<bool>(
                name: "IsCleanedUp",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Migrate legacy FinalisedAndCleanedUp rows: restore Status to Finalised and mark IsCleanedUp.
            // FinalisedAndCleanedUp has been removed from the enum; Status is stored as nvarchar.
            migrationBuilder.Sql(@"
                UPDATE [MatchPrediction].[ParallelMatchPredictionRuns]
                SET [IsCleanedUp] = 1, [Status] = 'Finalised'
                WHERE [Status] = 'FinalisedAndCleanedUp'
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ParallelMatchPredictionRuns_IsCleanedUp_MatchPredictionRunInitiatedUtc",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns",
                columns: new[] { "IsCleanedUp", "MatchPredictionRunInitiatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ParallelMatchPredictionRuns_Status",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParallelMatchPredictionRuns_IsCleanedUp_MatchPredictionRunInitiatedUtc",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns");

            migrationBuilder.DropIndex(
                name: "IX_ParallelMatchPredictionRuns_Status",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns");

            // Best-effort restore of the legacy status before the IsCleanedUp column is dropped, so older
            // code that still expects FinalisedAndCleanedUp does not misread these historical rows.
            // In the pre-migration world a finalised-and-cleaned run *was* FinalisedAndCleanedUp, so mapping
            // (Finalised + IsCleanedUp) back to that value is the faithful inverse. Cleaned-up rows in other
            // statuses (failed/abandoned) had no equivalent legacy value and are left as-is.
            migrationBuilder.Sql(@"
                UPDATE [MatchPrediction].[ParallelMatchPredictionRuns]
                SET [Status] = 'FinalisedAndCleanedUp'
                WHERE [Status] = 'Finalised' AND [IsCleanedUp] = 1
            ");

            migrationBuilder.DropColumn(
                name: "IsCleanedUp",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns");

            migrationBuilder.CreateIndex(
                name: "IX_ParallelMatchPredictionRuns_Status_FinalisedTimeUtc",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionRuns",
                columns: new[] { "Status", "FinalisedTimeUtc" });
        }
    }
}
