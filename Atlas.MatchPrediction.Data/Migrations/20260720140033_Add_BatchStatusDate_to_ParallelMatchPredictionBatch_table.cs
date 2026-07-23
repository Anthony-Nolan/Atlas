using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_BatchStatusDate_to_ParallelMatchPredictionBatch_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BatchStatusDate",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchStatusDate",
                schema: "MatchPrediction",
                table: "ParallelMatchPredictionBatches");
        }
    }
}
