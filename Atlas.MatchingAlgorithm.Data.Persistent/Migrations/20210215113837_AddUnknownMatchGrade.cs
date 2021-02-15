using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddUnknownMatchGrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                columns: new[] { "Id", "Name", "Weight" },
                values: new object[] { 15, "Unknown", 0 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
