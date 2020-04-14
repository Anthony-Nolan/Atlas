using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddFilteredIndexOnDonorsWithoutLocusC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "FI_DonorIdsWithoutLocusC",
                table: "Donors",
                column: "DonorId",
                filter: "[C_1] IS NULL AND [C_2] IS NULL")
                .Annotation("SqlServer:Include", new[] { "C_1", "C_2" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "FI_DonorIdsWithoutLocusC",
                table: "Donors");
        }
    }
}
