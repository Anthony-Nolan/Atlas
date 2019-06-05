using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.SearchAlgorithm.Data.Migrations
{
    public partial class MigrateToEFCore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Donors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    DonorType = table.Column<int>(nullable: false),
                    RegistryCode = table.Column<int>(nullable: false),
                    A_1 = table.Column<string>(nullable: true),
                    A_2 = table.Column<string>(nullable: true),
                    B_1 = table.Column<string>(nullable: true),
                    B_2 = table.Column<string>(nullable: true),
                    C_1 = table.Column<string>(nullable: true),
                    C_2 = table.Column<string>(nullable: true),
                    DPB1_1 = table.Column<string>(nullable: true),
                    DPB1_2 = table.Column<string>(nullable: true),
                    DQB1_1 = table.Column<string>(nullable: true),
                    DQB1_2 = table.Column<string>(nullable: true),
                    DRB1_1 = table.Column<string>(nullable: true),
                    DRB1_2 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PGroupName",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PGroupName", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtA",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHlaAtA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingHlaAtA_PGroupName_PGroupId",
                        column: x => x.PGroupId,
                        principalTable: "PGroupName",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtB",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHlaAtB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingHlaAtB_PGroupName_PGroupId",
                        column: x => x.PGroupId,
                        principalTable: "PGroupName",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtC",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHlaAtC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingHlaAtC_PGroupName_PGroupId",
                        column: x => x.PGroupId,
                        principalTable: "PGroupName",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtDQB1",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHlaAtDQB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingHlaAtDQB1_PGroupName_PGroupId",
                        column: x => x.PGroupId,
                        principalTable: "PGroupName",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtDRB1",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingHlaAtDRB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingHlaAtDRB1_PGroupName_PGroupId",
                        column: x => x.PGroupId,
                        principalTable: "PGroupName",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtA_PGroupId",
                table: "MatchingHlaAtA",
                column: "PGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtB_PGroupId",
                table: "MatchingHlaAtB",
                column: "PGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtC_PGroupId",
                table: "MatchingHlaAtC",
                column: "PGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDQB1_PGroupId",
                table: "MatchingHlaAtDQB1",
                column: "PGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDRB1_PGroupId",
                table: "MatchingHlaAtDRB1",
                column: "PGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Donors");

            migrationBuilder.DropTable(
                name: "MatchingHlaAtA");

            migrationBuilder.DropTable(
                name: "MatchingHlaAtB");

            migrationBuilder.DropTable(
                name: "MatchingHlaAtC");

            migrationBuilder.DropTable(
                name: "MatchingHlaAtDQB1");

            migrationBuilder.DropTable(
                name: "MatchingHlaAtDRB1");

            migrationBuilder.DropTable(
                name: "PGroupName");
        }
    }
}
