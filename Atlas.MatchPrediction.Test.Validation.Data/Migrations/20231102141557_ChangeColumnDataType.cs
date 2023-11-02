using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class ChangeColumnDataType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ExternalHfSetId",
                table: "SubjectInfo",
                type: "int",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalHfSetId",
                table: "SubjectInfo",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
