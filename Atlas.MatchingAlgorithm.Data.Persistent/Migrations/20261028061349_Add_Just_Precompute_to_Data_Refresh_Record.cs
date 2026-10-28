using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class Add_Just_Precompute_to_Data_Refresh_Record : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "JustPrecompute",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JustPrecompute",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
