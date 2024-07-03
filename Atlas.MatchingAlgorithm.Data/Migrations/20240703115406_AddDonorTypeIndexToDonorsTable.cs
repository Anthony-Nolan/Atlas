using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddDonorTypeIndexToDonorsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Donors_DonorType",
                table: "Donors",
                column: "DonorType")
                .Annotation("SqlServer:Include", new[] { "DonorId", "RegistryCode" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_DonorType",
                table: "Donors");
        }
    }
}
