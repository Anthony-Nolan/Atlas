using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Donors",
                columns: table => new
                {
                    AtlasId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalDonorCode = table.Column<string>(maxLength: 64, nullable: true),
                    DonorType = table.Column<int>(nullable: false),
                    EthnicityCode = table.Column<string>(maxLength: 256, nullable: true),
                    RegistryCode = table.Column<string>(maxLength: 256, nullable: true),
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
                    DRB1_2 = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.AtlasId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donors_ExternalDonorCode",
                table: "Donors",
                column: "ExternalDonorCode",
                unique: true,
                filter: "[ExternalDonorCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Donors_Hash",
                table: "Donors",
                column: "Hash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Donors");
        }
    }
}
