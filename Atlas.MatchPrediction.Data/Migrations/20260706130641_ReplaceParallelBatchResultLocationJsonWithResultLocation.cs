using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceParallelBatchResultLocationJsonWithResultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultLocationJson",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches");

            migrationBuilder.AddColumn<string>(
                name: "ResultLocation",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultLocation",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches");

            migrationBuilder.AddColumn<string>(
                name: "ResultLocationJson",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
