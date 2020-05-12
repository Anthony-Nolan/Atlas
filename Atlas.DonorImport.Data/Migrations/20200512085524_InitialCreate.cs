using Microsoft.EntityFrameworkCore.Metadata;
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
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    DonorType = table.Column<int>(nullable: false),
                    Ethnicity = table.Column<string>(nullable: true),
                    RegistryCode = table.Column<int>(nullable: false),
                    A_1 = table.Column<string>(nullable: true),
                    A_2 = table.Column<string>(nullable: true),
                    B_1 = table.Column<string>(nullable: true),
                    B_2 = table.Column<string>(nullable: true),
                    C_1 = table.Column<string>(nullable: true),
                    C_2 = table.Column<string>(nullable: true),
                    DPB_1 = table.Column<string>(nullable: true),
                    DPB_2 = table.Column<string>(nullable: true),
                    DQB1_1 = table.Column<string>(nullable: true),
                    DQB1_2 = table.Column<string>(nullable: true),
                    DRB1_1 = table.Column<string>(nullable: true),
                    DRB1_2 = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Donors");
        }
    }
}
