using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.SearchAlgorithm.Data.Migrations
{
    public partial class AddIsAvailableForSearchPropertyToDonorModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForSearch",
                table: "Donors",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailableForSearch",
                table: "Donors");
        }
    }
}
