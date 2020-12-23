using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class NormaliseMatchingData_PerLocusRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelations");

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelationAtA",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelationAtA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtA_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtA_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelationAtB",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelationAtB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtB_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtB_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelationAtC",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelationAtC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtC_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtC_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelationAtDQB1",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelationAtDQB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtDQB1_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtDQB1_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelationAtDRB1",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelationAtDRB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtDRB1_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelationAtDRB1_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtA_HlaName_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtA_PGroup_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtB_HlaName_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtB_PGroup_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtC_HlaName_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtC_PGroup_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "PGroup_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(type: "int", nullable: true),
                    PGroup_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelations_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelations_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelations_HlaName_Id",
                table: "HlaNamePGroupRelations",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelations_PGroup_Id",
                table: "HlaNamePGroupRelations",
                column: "PGroup_Id");
        }
    }
}
