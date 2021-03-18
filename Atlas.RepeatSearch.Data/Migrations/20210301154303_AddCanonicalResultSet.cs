using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.RepeatSearch.Data.Migrations
{
    public partial class AddCanonicalResultSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "RepeatSearch");

            migrationBuilder.CreateTable(
                name: "CanonicalResultSets",
                schema: "RepeatSearch",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalSearchRequestId = table.Column<string>(maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalResultSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchResults",
                schema: "RepeatSearch",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalResultSetId = table.Column<int>(nullable: false),
                    AtlasDonorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchResults_CanonicalResultSets_CanonicalResultSetId",
                        column: x => x.CanonicalResultSetId,
                        principalSchema: "RepeatSearch",
                        principalTable: "CanonicalResultSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalResultSets_OriginalSearchRequestId",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                column: "OriginalSearchRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchResults_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults",
                column: "CanonicalResultSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResults_AtlasDonorId_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResults",
                columns: new[] { "AtlasDonorId", "CanonicalResultSetId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchResults",
                schema: "RepeatSearch");

            migrationBuilder.DropTable(
                name: "CanonicalResultSets",
                schema: "RepeatSearch");
        }
    }
}
