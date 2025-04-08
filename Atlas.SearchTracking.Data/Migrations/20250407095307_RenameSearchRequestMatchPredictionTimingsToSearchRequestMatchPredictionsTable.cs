using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class RenameSearchRequestMatchPredictionTimingsToSearchRequestMatchPredictionsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SearchRequestMatchPredictionTimings",
                schema: "SearchTracking",
                newName: "SearchRequestMatchPredictions",
                newSchema: "SearchTracking");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequestMatchPredictionTimings_SearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions",
                newName: "IX_SearchRequestMatchPredictions_SearchRequestId");

            migrationBuilder.RenameIndex(
                name: "PK_SearchRequestMatchPredictionTimings",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions",
                newName: "PK_SearchRequestMatchPredictions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SearchRequestMatchPredictions",
                schema: "SearchTracking",
                newName: "SearchRequestMatchPredictionTimings",
                newSchema: "SearchTracking");

            migrationBuilder.RenameIndex(
                name: "PK_SearchRequestMatchPredictions",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                newName: "PK_SearchRequestMatchPredictionTimings");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequestMatchPredictions_SearchRequestId",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                newName: "IX_SearchRequestMatchPredictionTimings_SearchRequestId");
        }
    }
}
