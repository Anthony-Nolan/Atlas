using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class UpdateMatchGradesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new match grade - manually choosing the `Id` value to avoid clashing with already seeded data
            migrationBuilder.InsertData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                columns: new[] { "Id", "Name", "Weight" },
                values: new object[] { 16, "ExpressingVsNull", 0 });

            // This match grade ("PermissiveMismatch") was removed from the MatchGrades enum several commits ago but had not been removed from the db
            migrationBuilder.DeleteData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                keyColumn: "Id",
                keyValue: 2);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.InsertData(
                schema: "MatchingAlgorithmPersistent",
                table: "GradeWeightings",
                columns: new[] { "Id", "Name", "Weight" },
                values: new object[] { 2, "PermissiveMismatch", 0 });
        }
    }
}
