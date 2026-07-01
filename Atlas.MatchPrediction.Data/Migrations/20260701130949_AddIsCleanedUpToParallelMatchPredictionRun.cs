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
            // StatusDateUtc is realigned to FinalisedTimeUtc: under the new model the cleanup step no longer
            // touches Status/StatusDateUtc, so a finalised-and-cleaned run's StatusDateUtc equals its
            // finalisation moment. The legacy rows instead carried the cleanup time, which would leave the
            // (Status='Finalised', StatusDateUtc) pair inconsistent. COALESCE keeps the existing value on the
            // (not expected) chance FinalisedTimeUtc is null.
            migrationBuilder.Sql(@"
                UPDATE [MatchPrediction].[ParallelMatchPredictionRuns]
                SET [IsCleanedUp] = 1,
                    [Status] = 'Finalised',
                    [StatusDateUtc] = COALESCE([FinalisedTimeUtc], [StatusDateUtc])
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

            // Restore the pre-migration invariant "no batch rows implies FinalisedAndCleanedUp" before the
            // IsCleanedUp column is dropped. Mapping cleaned-up Finalised rows back is the faithful inverse;
            // mapping cleaned-up Running rows is a correctness fix — old code's finalisation query treats a
            // batch-less Running run as ready (All(...) over an empty set is vacuously true) and would
            // reprocess it. Failed runs are left as-is: terminal and never re-queried. StatusDateUtc is
            // stamped with the rollback time, the moment this status change happens.
            migrationBuilder.Sql(@"
                UPDATE [MatchPrediction].[ParallelMatchPredictionRuns]
                SET [Status] = 'FinalisedAndCleanedUp',
                    [StatusDateUtc] = SYSUTCDATETIME()
                WHERE ([Status] = 'Finalised' OR [Status] = 'Running') AND [IsCleanedUp] = 1
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
