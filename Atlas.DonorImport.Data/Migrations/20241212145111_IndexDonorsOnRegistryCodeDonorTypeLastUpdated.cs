using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class IndexDonorsOnRegistryCodeDonorTypeLastUpdated : Migration
    {
        private const string createIndexSql = @"
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donors_RegistryCode_DonorType_LastUpdated' AND object_id = OBJECT_ID('Donors.Donors'))
            BEGIN
	            CREATE INDEX [IX_Donors_RegistryCode_DonorType_LastUpdated] ON Donors.Donors([RegistryCode], [DonorType], [LastUpdated])
                INCLUDE ([ExternalDonorCode]) WITH (ONLINE = ON);
            END
            ";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(createIndexSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_RegistryCode_DonorType_LastUpdated",
                schema: "Donors",
                table: "Donors");
        }
    }
}
