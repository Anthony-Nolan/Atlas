using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AddIsParallelMatchPredictionToSearchRequestMatchPredictions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsParallelMatchPrediction",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsParallelMatchPrediction",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions");
        }
    }
}
