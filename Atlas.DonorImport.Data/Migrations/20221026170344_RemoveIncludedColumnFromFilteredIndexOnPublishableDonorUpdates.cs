using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class RemoveIncludedColumnFromFilteredIndexOnPublishableDonorUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates");

            migrationBuilder.CreateIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                column: "IsPublished",
                filter: "[IsPublished] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates");

            migrationBuilder.CreateIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                column: "IsPublished",
                filter: "[IsPublished] = 0")
                .Annotation("SqlServer:Include", new[] { "SearchableDonorUpdate" });
        }
    }
}
