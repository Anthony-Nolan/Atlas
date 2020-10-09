using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyVerificationRunsTable_DropSearchRequestColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchRequest",
                table: "VerificationRuns");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchRequest",
                table: "VerificationRuns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
