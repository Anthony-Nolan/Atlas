using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddFilteredIndexOnDonorsWithoutLocusDqb1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "FI_DonorIdsWithoutLocusDQB1",
                table: "Donors",
                column: "DonorId",
                filter: "[DQB1_1] IS NULL AND [DQB1_2] IS NULL")
                .Annotation("SqlServer:Include", new[] { "DQB1_1", "DQB1_2" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "FI_DonorIdsWithoutLocusDQB1",
                table: "Donors");
        }
    }
}
