using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class RenameEthnicityColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ethnicity",
                table: "Donors");

            migrationBuilder.AddColumn<string>(
                name: "EthnicityCode",
                table: "Donors",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EthnicityCode",
                table: "Donors");

            migrationBuilder.AddColumn<string>(
                name: "Ethnicity",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
