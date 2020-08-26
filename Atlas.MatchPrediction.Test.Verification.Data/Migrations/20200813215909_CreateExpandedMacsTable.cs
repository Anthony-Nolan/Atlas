using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class CreateExpandedMacsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpandedMacs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecondField = table.Column<string>(type: "nvarchar(8)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpandedMacs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpandedMacs_SecondField",
                table: "ExpandedMacs",
                column: "SecondField")
                .Annotation("SqlServer:Include", new[] { "Code" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpandedMacs");
        }
    }
}
