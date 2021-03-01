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
                name: "SearchResult",
                schema: "RepeatSearch",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtlasDonorId = table.Column<int>(nullable: false),
                    CanonicalResultSetId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchResult_CanonicalResultSets_CanonicalResultSetId",
                        column: x => x.CanonicalResultSetId,
                        principalSchema: "RepeatSearch",
                        principalTable: "CanonicalResultSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalResultSets_OriginalSearchRequestId",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                column: "OriginalSearchRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchResult_AtlasDonorId",
                schema: "RepeatSearch",
                table: "SearchResult",
                column: "AtlasDonorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchResult_CanonicalResultSetId",
                schema: "RepeatSearch",
                table: "SearchResult",
                column: "CanonicalResultSetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchResult",
                schema: "RepeatSearch");

            migrationBuilder.DropTable(
                name: "CanonicalResultSets",
                schema: "RepeatSearch");
        }
    }
}
