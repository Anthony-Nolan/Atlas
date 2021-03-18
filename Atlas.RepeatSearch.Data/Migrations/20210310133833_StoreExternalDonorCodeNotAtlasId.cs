using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.RepeatSearch.Data.Migrations
{
    public partial class StoreExternalDonorCodeNotAtlasId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchResults_AtlasDonorId_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults");

            migrationBuilder.DropColumn(
                name: "AtlasDonorId",
                schema: "RepeatSearch",
                table: "SearchResults");

            migrationBuilder.AddColumn<string>(
                name: "ExternalDonorCode",
                schema: "RepeatSearch",
                table: "SearchResults",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResults_ExternalDonorCode_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults",
                columns: new[] { "ExternalDonorCode", "CanonicalResultSetId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchResults_ExternalDonorCode_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults");

            migrationBuilder.DropColumn(
                name: "ExternalDonorCode",
                schema: "RepeatSearch",
                table: "SearchResults");

            migrationBuilder.AddColumn<int>(
                name: "AtlasDonorId",
                schema: "RepeatSearch",
                table: "SearchResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SearchResults_AtlasDonorId_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults",
                columns: new[] { "AtlasDonorId", "CanonicalResultSetId" },
                unique: true);
        }
    }
}
