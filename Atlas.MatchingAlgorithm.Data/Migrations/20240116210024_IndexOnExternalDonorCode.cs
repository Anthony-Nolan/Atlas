using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class IndexOnExternalDonorCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalDonorCode",
                table: "Donors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_ExternalDonorCode",
                table: "Donors",
                column: "ExternalDonorCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_ExternalDonorCode",
                table: "Donors");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalDonorCode",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }
    }
}
