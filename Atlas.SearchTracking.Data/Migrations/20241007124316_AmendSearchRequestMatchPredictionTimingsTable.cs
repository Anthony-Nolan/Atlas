using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AmendSearchRequestMatchPredictionTimingsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureInfo_Message",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchPrediction_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttempts",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureInfo_ExceptionStacktrace",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings");

            migrationBuilder.DropColumn(
                name: "FailureInfo_Message",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings");

            migrationBuilder.DropColumn(
                name: "MatchPrediction_FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictionTimings");

            migrationBuilder.AlterColumn<int>(
                name: "FailureInfo_Type",
                schema: "SearchTracking",
                table: "SearchRequestMatchingAlgorithmAttempts",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);
        }
    }
}
