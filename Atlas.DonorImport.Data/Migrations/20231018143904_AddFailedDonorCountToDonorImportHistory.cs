using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddFailedDonorCountToDonorImportHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedDonorCount",
                schema: "Donors",
                table: "DonorImportHistory",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedDonorCount",
                schema: "Donors",
                table: "DonorImportHistory");
        }
    }
}
