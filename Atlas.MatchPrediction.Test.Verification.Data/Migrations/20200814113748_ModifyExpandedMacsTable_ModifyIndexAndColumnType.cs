using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifyExpandedMacsTable_ModifyIndexAndColumnType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpandedMacs_SecondField",
                table: "ExpandedMacs");

            migrationBuilder.AlterColumn<string>(
                name: "SecondField",
                table: "ExpandedMacs",
                type: "nvarchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ExpandedMacs",
                type: "nvarchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ExpandedMacs_Code_SecondField",
                table: "ExpandedMacs",
                columns: new[] { "Code", "SecondField" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpandedMacs_Code_SecondField",
                table: "ExpandedMacs");

            migrationBuilder.AlterColumn<string>(
                name: "SecondField",
                table: "ExpandedMacs",
                type: "nvarchar(8)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ExpandedMacs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)");

            migrationBuilder.CreateIndex(
                name: "IX_ExpandedMacs_SecondField",
                table: "ExpandedMacs",
                column: "SecondField")
                .Annotation("SqlServer:Include", new[] { "Code" });
        }
    }
}
