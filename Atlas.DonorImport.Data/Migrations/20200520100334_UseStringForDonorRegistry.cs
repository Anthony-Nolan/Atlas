using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class UseStringForDonorRegistry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistryCode",
                table: "Donors");

            migrationBuilder.AddColumn<string>(
                name: "Registry",
                table: "Donors",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Registry",
                table: "Donors");

            migrationBuilder.AddColumn<int>(
                name: "RegistryCode",
                table: "Donors",
                nullable: false,
                defaultValue: 0);
        }
    }
}
