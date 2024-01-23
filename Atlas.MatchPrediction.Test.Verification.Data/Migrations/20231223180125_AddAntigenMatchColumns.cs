using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class AddAntigenMatchColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAntigenMatch_1",
                table: "LocusMatchDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAntigenMatch_2",
                table: "LocusMatchDetails",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAntigenMatch_1",
                table: "LocusMatchDetails");

            migrationBuilder.DropColumn(
                name: "IsAntigenMatch_2",
                table: "LocusMatchDetails");
        }
    }
}
