using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifySearchRequestsTable_AddSearchTimesColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "MatchPredictionTime",
                table: "SearchRequests",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MatchingAlgorithmTime",
                table: "SearchRequests",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OverallSearchTime",
                table: "SearchRequests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchPredictionTime",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "MatchingAlgorithmTime",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "OverallSearchTime",
                table: "SearchRequests");
        }
    }
}
