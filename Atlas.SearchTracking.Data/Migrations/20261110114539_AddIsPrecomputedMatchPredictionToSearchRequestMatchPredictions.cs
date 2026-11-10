using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPrecomputedMatchPredictionToSearchRequestMatchPredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrecomputedMatchPrediction",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrecomputedMatchPrediction",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions");
        }
    }
}
