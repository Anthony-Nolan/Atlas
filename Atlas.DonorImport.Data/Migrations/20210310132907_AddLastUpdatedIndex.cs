using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddLastUpdatedIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Donors_LastUpdated_ExternalDonorCode",
                schema: "Donors",
                table: "Donors",
                columns: new[] { "LastUpdated", "ExternalDonorCode" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_LastUpdated_ExternalDonorCode",
                schema: "Donors",
                table: "Donors");
        }
    }
}
