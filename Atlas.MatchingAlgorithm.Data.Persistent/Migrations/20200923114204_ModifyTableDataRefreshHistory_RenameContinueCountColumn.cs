using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class ModifyTableDataRefreshHistory_RenameContinueCountColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshContinuedCount",
                newName: "RefreshAttemptedCount",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshAttemptedCount",
                newName: "RefreshContinuedCount",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
