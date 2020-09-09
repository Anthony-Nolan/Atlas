using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MatchingAlgorithmPersistent");

            migrationBuilder.RenameTable(
                name: "GradeWeightings",
                newName: "GradeWeightings",
                newSchema: "MatchingAlgorithmPersistent");

            migrationBuilder.RenameTable(
                name: "DataRefreshHistory",
                newName: "DataRefreshHistory",
                newSchema: "MatchingAlgorithmPersistent");

            migrationBuilder.RenameTable(
                name: "ConfidenceWeightings",
                newName: "ConfidenceWeightings",
                newSchema: "MatchingAlgorithmPersistent");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "GradeWeightings",
                schema: "MatchingAlgorithmPersistent",
                newName: "GradeWeightings");

            migrationBuilder.RenameTable(
                name: "DataRefreshHistory",
                schema: "MatchingAlgorithmPersistent",
                newName: "DataRefreshHistory");

            migrationBuilder.RenameTable(
                name: "ConfidenceWeightings",
                schema: "MatchingAlgorithmPersistent",
                newName: "ConfidenceWeightings");
        }
    }
}
