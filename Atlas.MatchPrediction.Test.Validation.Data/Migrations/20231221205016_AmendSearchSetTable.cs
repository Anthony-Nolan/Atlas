using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class AmendSearchSetTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "SearchRequestsSubmitted",
                table: "SearchSets",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "DonorType",
                table: "SearchSets",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatchLoci",
                table: "SearchSets",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MismatchCount",
                table: "SearchSets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorType",
                table: "SearchSets");

            migrationBuilder.DropColumn(
                name: "MatchLoci",
                table: "SearchSets");

            migrationBuilder.DropColumn(
                name: "MismatchCount",
                table: "SearchSets");

            migrationBuilder.AlterColumn<bool>(
                name: "SearchRequestsSubmitted",
                table: "SearchSets",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}
