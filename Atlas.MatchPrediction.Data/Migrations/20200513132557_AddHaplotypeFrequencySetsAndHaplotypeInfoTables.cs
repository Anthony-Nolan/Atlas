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
                name: "HaplotypeFrequencies",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
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
                    table.PrimaryKey("PK_HaplotypeFrequencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HaplotypeFrequencies_HaplotypeFrequencySets_Set_Id",
                        column: x => x.Set_Id,
                        principalTable: "HaplotypeFrequencySets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_Set_Id",
                table: "HaplotypeFrequencies",
                column: "Set_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RegistryAndEthnicity",
                table: "HaplotypeFrequencySets",
                columns: new[] { "Ethnicity", "Registry" },
                unique: true,
                filter: "[Active] = 'True'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HaplotypeFrequencies");

            migrationBuilder.DropTable(
                name: "HaplotypeFrequencySets");
        }
    }
}
