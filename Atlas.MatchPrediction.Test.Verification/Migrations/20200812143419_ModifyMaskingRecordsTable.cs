using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Migrations
{
    public partial class ModifyMaskingRecordsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProportionDeleted",
                table: "MaskingRecords");

            migrationBuilder.RenameColumn(
                name: "ProportionsMasked",
                table: "MaskingRecords",
                newName: "MaskingRequests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaskingRequests",
                table: "MaskingRecords",
                newName: "ProportionsMasked");

            migrationBuilder.AddColumn<int>(
                name: "ProportionDeleted",
                table: "MaskingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
