using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddMaxLengthForHaplotypeFrequencyData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistryAndEthnicity",
                table: "HaplotypeFrequencySets");

            migrationBuilder.DropColumn(
                name: "Ethnicity",
                table: "HaplotypeFrequencySets");

            migrationBuilder.DropColumn(
                name: "Registry",
                table: "HaplotypeFrequencySets");

            migrationBuilder.AddColumn<string>(
                name: "EthnicityCode",
                table: "HaplotypeFrequencySets",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistryCode",
                table: "HaplotypeFrequencySets",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DRB1",
                table: "HaplotypeFrequencies",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DQB1",
                table: "HaplotypeFrequencies",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "C",
                table: "HaplotypeFrequencies",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "B",
                table: "HaplotypeFrequencies",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "A",
                table: "HaplotypeFrequencies",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_RegistryCode_And_EthnicityCode",
                table: "HaplotypeFrequencySets",
                columns: new[] { "EthnicityCode", "RegistryCode" },
                unique: true,
                filter: "[Active] = 'True'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistryCode_And_EthnicityCode",
                table: "HaplotypeFrequencySets");

            migrationBuilder.DropColumn(
                name: "EthnicityCode",
                table: "HaplotypeFrequencySets");

            migrationBuilder.DropColumn(
                name: "RegistryCode",
                table: "HaplotypeFrequencySets");

            migrationBuilder.AddColumn<string>(
                name: "Ethnicity",
                table: "HaplotypeFrequencySets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Registry",
                table: "HaplotypeFrequencySets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DRB1",
                table: "HaplotypeFrequencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DQB1",
                table: "HaplotypeFrequencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "C",
                table: "HaplotypeFrequencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "B",
                table: "HaplotypeFrequencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "A",
                table: "HaplotypeFrequencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_RegistryAndEthnicity",
                table: "HaplotypeFrequencySets",
                columns: new[] { "Ethnicity", "Registry" },
                unique: true,
                filter: "[Active] = 'True'");
        }
    }
}
