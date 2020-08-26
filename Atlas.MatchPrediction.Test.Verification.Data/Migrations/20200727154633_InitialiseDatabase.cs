using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class InitialiseDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NormalisedPool",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDateTime = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Comments = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalisedPool", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalisedHaplotypeFrequencies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalisedPool_Id = table.Column<int>(nullable: false),
                    A = table.Column<string>(maxLength: 64, nullable: false),
                    B = table.Column<string>(maxLength: 64, nullable: false),
                    C = table.Column<string>(maxLength: 64, nullable: false),
                    DQB1 = table.Column<string>(maxLength: 64, nullable: false),
                    DRB1 = table.Column<string>(maxLength: 64, nullable: false),
                    Frequency = table.Column<decimal>(type: "decimal(20,20)", nullable: false),
                    CopyNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalisedHaplotypeFrequencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NormalisedHaplotypeFrequencies_NormalisedPool_NormalisedPool_Id",
                        column: x => x.NormalisedPool_Id,
                        principalTable: "NormalisedPool",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestHarnesses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDateTime = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Comments = table.Column<string>(nullable: true),
                    NormalisedPool_Id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestHarnesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestHarnesses_NormalisedPool_NormalisedPool_Id",
                        column: x => x.NormalisedPool_Id,
                        principalTable: "NormalisedPool",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Simulants",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestHarness_Id = table.Column<int>(nullable: false),
                    TestIndividualCategory = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    SimulatedHlaTypingCategory = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    A_1 = table.Column<string>(maxLength: 64, nullable: false),
                    A_2 = table.Column<string>(maxLength: 64, nullable: false),
                    B_1 = table.Column<string>(maxLength: 64, nullable: false),
                    B_2 = table.Column<string>(maxLength: 64, nullable: false),
                    C_1 = table.Column<string>(maxLength: 64, nullable: false),
                    C_2 = table.Column<string>(maxLength: 64, nullable: false),
                    DQB1_1 = table.Column<string>(maxLength: 64, nullable: false),
                    DQB1_2 = table.Column<string>(maxLength: 64, nullable: false),
                    DRB1_1 = table.Column<string>(maxLength: 64, nullable: false),
                    DRB1_2 = table.Column<string>(maxLength: 64, nullable: false),
                    SourceSimulantId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Simulants_TestHarnesses_TestHarness_Id",
                        column: x => x.TestHarness_Id,
                        principalTable: "TestHarnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NormalisedHaplotypeFrequencies_NormalisedPool_Id",
                table: "NormalisedHaplotypeFrequencies",
                column: "NormalisedPool_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Simulants_TestHarness_Id",
                table: "Simulants",
                column: "TestHarness_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Simulants_TestIndividualCategory_SimulatedHlaTypingCategory",
                table: "Simulants",
                columns: new[] { "TestIndividualCategory", "SimulatedHlaTypingCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_TestHarnesses_NormalisedPool_Id",
                table: "TestHarnesses",
                column: "NormalisedPool_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NormalisedHaplotypeFrequencies");

            migrationBuilder.DropTable(
                name: "Simulants");

            migrationBuilder.DropTable(
                name: "TestHarnesses");

            migrationBuilder.DropTable(
                name: "NormalisedPool");
        }
    }
}
