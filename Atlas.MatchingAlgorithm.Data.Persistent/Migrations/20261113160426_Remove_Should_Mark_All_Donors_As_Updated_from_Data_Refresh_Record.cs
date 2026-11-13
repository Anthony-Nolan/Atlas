using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Should_Mark_All_Donors_As_Updated_from_Data_Refresh_Record : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShouldMarkAllDonorsAsUpdated",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShouldMarkAllDonorsAsUpdated",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
