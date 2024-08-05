using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class AddDonorTypeAndRegistryCodeIndex : Migration
    {
        private const string createIndexSql = @"
            if not exists (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donors_DonorType_RegistryCode' AND object_id = OBJECT_ID('dbo.Donors')) 
	            CREATE INDEX [IX_Donors_DonorType_RegistryCode] ON [Donors] ([DonorType], [RegistryCode]) WITH (ONLINE = ON);
            ";

        private const string changeColumnsTypeSql = @"
            ALTER TABLE [Donors] ALTER COLUMN [EthnicityCode] nvarchar(256) NULL;
            ALTER TABLE [Donors] ALTER COLUMN [RegistryCode] nvarchar(256) NULL;
            ";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<string>(
            //    name: "RegistryCode",
            //    table: "Donors",
            //    type: "nvarchar(256)",
            //    maxLength: 256,
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)",
            //    oldNullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "EthnicityCode",
            //    table: "Donors",
            //    type: "nvarchar(256)",
            //    maxLength: 256,
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)",
            //    oldNullable: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_Donors_DonorType_RegistryCode",
            //    table: "Donors",
            //    columns: new[] { "DonorType", "RegistryCode" });

            // Leaving original migration code above


            migrationBuilder.Sql(changeColumnsTypeSql);
            migrationBuilder.Sql(createIndexSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donors_DonorType_RegistryCode",
                table: "Donors");

            migrationBuilder.AlterColumn<string>(
                name: "RegistryCode",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EthnicityCode",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
