using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class MakeIX_SearchRequestId_And_AttemptNumberNonUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttemptTimings");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttemptTimings",
                columns: new[] { "SearchRequestId", "AttemptNumber" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttemptTimings");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequestId_And_AttemptNumber",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttemptTimings",
                columns: new[] { "SearchRequestId", "AttemptNumber" },
                unique: true);
        }
    }
}
