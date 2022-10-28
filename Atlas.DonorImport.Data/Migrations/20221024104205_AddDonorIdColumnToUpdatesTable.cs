using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddDonorIdColumnToUpdatesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DonorId",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorId",
                schema: "Donors",
                table: "PublishableDonorUpdates");
        }
    }
}
