using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddDonorIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                table: "Donors",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DonorId",
                table: "Donors",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_DonorId",
                table: "Donors",
                column: "DonorId",
                unique: true,
                filter: "[DonorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_Hash",
                table: "Donors",
                column: "Hash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_DonorId",
                table: "Donors");

            migrationBuilder.DropIndex(
                name: "IX_Donors_Hash",
                table: "Donors");

            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DonorId",
                table: "Donors",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
