using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RenameMatchCountsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchCounts_MatchedDonors_MatchedDonor_Id",
                table: "MatchCounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatchCounts",
                table: "MatchCounts");

            migrationBuilder.RenameTable(
                name: "MatchCounts",
                newName: "LocusMatchDetails");

            migrationBuilder.RenameIndex(
                name: "IX_MatchCounts_MatchedDonor_Id_Locus_MatchCount",
                table: "LocusMatchDetails",
                newName: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocusMatchDetails",
                table: "LocusMatchDetails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocusMatchDetails_MatchedDonors_MatchedDonor_Id",
                table: "LocusMatchDetails",
                column: "MatchedDonor_Id",
                principalTable: "MatchedDonors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocusMatchDetails_MatchedDonors_MatchedDonor_Id",
                table: "LocusMatchDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocusMatchDetails",
                table: "LocusMatchDetails");

            migrationBuilder.RenameTable(
                name: "LocusMatchDetails",
                newName: "MatchCounts");

            migrationBuilder.RenameIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount",
                table: "MatchCounts",
                newName: "IX_MatchCounts_MatchedDonor_Id_Locus_MatchCount");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatchCounts",
                table: "MatchCounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchCounts_MatchedDonors_MatchedDonor_Id",
                table: "MatchCounts",
                column: "MatchedDonor_Id",
                principalTable: "MatchedDonors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
