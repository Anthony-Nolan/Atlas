using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class Add_Manual_Decision_Columns_to_Data_Refresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AwaitingManualDecisionSinceUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingManualDecision",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwaitingManualDecisionSinceUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "PendingManualDecision",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
