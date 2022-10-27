using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddNewFilteredIndexToPublishableDonorUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PublishableDonorUpdates_PublishedOn_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                columns: new[] { "PublishedOn", "IsPublished" },
                filter: "[IsPublished] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishableDonorUpdates_PublishedOn_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates");
        }
    }
}
