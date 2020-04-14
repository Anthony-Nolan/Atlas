using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class MigrateToEFCore : Migration
    {
        // Note that key/index names have been changed manually to match those generated in existing databases by EF6
        // Some indexes have also been added manually to this migration to match the indexes applied in live - these should be removed and re-added when re-processing data
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
                    table.PrimaryKey("PK_dbo.Donors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PGroupNames",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.PGroupNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchingHlaAtA",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    TypePosition = table.Column<int>(nullable: false),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.MatchingHlaAtA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.MatchingHlaAtA_dbo.PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
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
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.MatchingHlaAtB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.MatchingHlaAtB_dbo.PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
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
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.MatchingHlaAtC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.MatchingHlaAtC_dbo.PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
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
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.MatchingHlaAtDQB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.MatchingHlaAtDQB1_dbo.PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
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
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.MatchingHlaAtDRB1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.MatchingHlaAtDRB1_dbo.PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtA",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtB",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtC",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDQB1",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PGroup_Id",
                table: "MatchingHlaAtDRB1",
                column: "PGroup_Id");
            
            // These indexes have also been added manually to this migration to match the indexes applied in live at the time of the .net core migration
            // These should be removed and re-added when re-processing data
            migrationBuilder.Sql(@"
CREATE INDEX IX_PGroup_Id_DonorId__TypePosition
ON MatchingHlaAtA (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX IX_PGroup_Id_DonorId__TypePosition
ON MatchingHlaAtB (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX IX_PGroup_Id_DonorId__TypePosition
ON MatchingHlaAtC (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX IX_PGroup_Id_DonorId__TypePosition
ON MatchingHlaAtDrb1 (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX IX_PGroup_Id_DonorId__TypePosition
ON MatchingHlaAtDqb1 (PGroup_Id, DonorId)
INCLUDE (TypePosition)


CREATE INDEX IX_DonorId__PGroup_Id_TypePosition
ON MatchingHlaAtA (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX IX_DonorId__PGroup_Id_TypePosition
ON MatchingHlaAtB (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX IX_DonorId__PGroup_Id_TypePosition
ON MatchingHlaAtC (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX IX_DonorId__PGroup_Id_TypePosition
ON MatchingHlaAtDrb1 (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX IX_DonorId__PGroup_Id_TypePosition
ON MatchingHlaAtDqb1 (DonorId)
INCLUDE (TypePosition, PGroup_Id)

    
CREATE INDEX IX_DonorId
ON Donors (DonorId)
    
CREATE INDEX IX_DonorType_RegistryCode__DonorId
ON Donors (DonorType, RegistryCode)
INCLUDE (DonorId)
");
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
                name: "PGroupNames");
        }
    }
}
