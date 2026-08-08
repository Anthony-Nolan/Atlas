using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_GenotypeCounts_to_ParallelMatchPredictionBatch_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonorGenotypeCounts",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientGenotypeCount",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorGenotypeCounts",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches");

            migrationBuilder.DropColumn(
                name: "PatientGenotypeCount",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches");
        }
    }
}
