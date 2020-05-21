using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class FixNamingOfDpb1Columns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DPB_1",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "DPB_2",
                table: "Donors");

            migrationBuilder.AddColumn<string>(
                name: "DPB1_1",
                table: "Donors",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DPB1_2",
                table: "Donors",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DPB1_1",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "DPB1_2",
                table: "Donors");

            migrationBuilder.AddColumn<string>(
                name: "DPB_1",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DPB_2",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
