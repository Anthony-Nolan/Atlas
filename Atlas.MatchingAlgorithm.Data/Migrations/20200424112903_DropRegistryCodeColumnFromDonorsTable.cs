using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class DropRegistryCodeColumnFromDonorsTable : Migration
    {
        private const string RegistryCodeContainingIndexName = "IX_DonorType_RegistryCode__DonorId";
        private const string DonorTypeIndexName = "IX_DonorType__DonorId";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                RegistryCodeContainingIndexName,
                "Donors");

            migrationBuilder.DropColumn(
                name: "RegistryCode",
                table: "Donors");

            migrationBuilder.Sql(@"
                CREATE INDEX " + DonorTypeIndexName + @"
                ON Donors (DonorType)
                INCLUDE (DonorId)
                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                DonorTypeIndexName,
                "Donors");

            // Note: the column can only be re-added as nullable, as there may be existing rows in the table; this is contrary to the original schema
            migrationBuilder.AddColumn<int>(
                            name: "RegistryCode",
                            table: "Donors",
                            nullable: true);

            migrationBuilder.Sql(@"
            CREATE INDEX " + RegistryCodeContainingIndexName + @"
            ON Donors(DonorType, RegistryCode)
            INCLUDE(DonorId)
            ");
        }
    }
}
