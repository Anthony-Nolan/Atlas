using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddHaplotypeFrequencySetsAndHaplotypeInfoTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HaplotypeFrequencySets",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Registry = table.Column<string>(nullable: true),
                    Ethnicity = table.Column<string>(nullable: true),
                    Active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HaplotypeFrequencySets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HaplotypeInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Set_Id = table.Column<int>(nullable: true),
                    Frequency = table.Column<decimal>(nullable: false),
                    A = table.Column<string>(nullable: false),
                    B = table.Column<string>(nullable: false),
                    C = table.Column<string>(nullable: false),
                    DQB1 = table.Column<string>(nullable: false),
                    DRB1 = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HaplotypeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HaplotypeInfo_HaplotypeFrequencySets_Set_Id",
                        column: x => x.Set_Id,
                        principalTable: "HaplotypeFrequencySets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistryAndEthnicity",
                table: "HaplotypeFrequencySets",
                columns: new[] { "Ethnicity", "Registry" },
                unique: true,
                filter: "[Active] = 'True'");

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeInfo_Set_Id",
                table: "HaplotypeInfo",
                column: "Set_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HaplotypeInfo");

            migrationBuilder.DropTable(
                name: "HaplotypeFrequencySets");
        }
    }
}
