using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifySearchRequestsTable_ChangeSearchTimeColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<double>(
                name: "MatchPredictionTimeInMs",
                table: "SearchRequests",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchingAlgorithmTimeInMs",
                table: "SearchRequests",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OverallSearchTimeInMs",
                table: "SearchRequests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchPredictionTimeInMs",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "MatchingAlgorithmTimeInMs",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "OverallSearchTimeInMs",
                table: "SearchRequests");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MatchPredictionTime",
                table: "SearchRequests",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MatchingAlgorithmTime",
                table: "SearchRequests",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OverallSearchTime",
                table: "SearchRequests",
                type: "time",
                nullable: true);
        }
    }
}
