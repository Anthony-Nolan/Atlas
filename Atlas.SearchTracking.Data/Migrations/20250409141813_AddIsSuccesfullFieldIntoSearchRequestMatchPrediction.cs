using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    public partial class AddIsSuccesfullFieldIntoSearchRequestMatchPrediction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                schema: "SearchTracking",
                table: "SearchRequestMatchPredictions");
        }
    }
}
