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
