using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.DonorImport.Data.Migrations
{
    public class _202004291641_Add_Donor_Import_Db : Migration
    {
        const string TABLE_NAME = "Donors";
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TABLE_NAME,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DonorId = table.Column<int>(nullable: false),
                    DonorType = table.Column<int>(nullable: false),
                    Ethnicity = table.Column<string>(nullable: false),
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
                    DRB1_2 = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.Donors", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: TABLE_NAME);
        }
    }
}
