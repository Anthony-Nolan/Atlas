using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class RenameDonorIdColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Donors",
                table: "Donors");

            migrationBuilder.DropIndex(
                name: "IX_Donors_DonorId",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "DonorId",
                table: "Donors");

            migrationBuilder.AddColumn<int>(
                name: "AtlasId",
                table: "Donors",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "ExternalDonorCode",
                table: "Donors",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Donors",
                table: "Donors",
                column: "AtlasId");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_ExternalDonorCode",
                table: "Donors",
                column: "ExternalDonorCode",
                unique: true,
                filter: "[ExternalDonorCode] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Donors",
                table: "Donors");

            migrationBuilder.DropIndex(
                name: "IX_Donors_ExternalDonorCode",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "AtlasId",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "ExternalDonorCode",
                table: "Donors");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "DonorId",
                table: "Donors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Donors",
                table: "Donors",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_DonorId",
                table: "Donors",
                column: "DonorId",
                unique: true,
                filter: "[DonorId] IS NOT NULL");
        }
    }
}
