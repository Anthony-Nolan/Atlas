using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddDonorImportFailures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorImportFailures",
                schema: "Donors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalDonorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DonorType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EthnicityCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RegistryCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdateFile = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdateProperty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FailureTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorImportFailures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorImportFailures_DonorType_EthnicityCode_RegistryCode_FailureReason_FailureTime",
                schema: "Donors",
                table: "DonorImportFailures",
                columns: new[] { "DonorType", "EthnicityCode", "RegistryCode", "FailureReason", "FailureTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DonorImportFailures_ExternalDonorCode",
                schema: "Donors",
                table: "DonorImportFailures",
                column: "ExternalDonorCode");

            migrationBuilder.CreateIndex(
                name: "IX_DonorImportFailures_UpdateFile",
                schema: "Donors",
                table: "DonorImportFailures",
                column: "UpdateFile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorImportFailures",
                schema: "Donors");
        }
    }
}
