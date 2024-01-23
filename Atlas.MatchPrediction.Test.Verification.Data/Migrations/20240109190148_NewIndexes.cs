using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class NewIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount",
                table: "LocusMatchDetails");

            migrationBuilder.CreateIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id",
                table: "LocusMatchDetails",
                column: "MatchedDonor_Id")
                .Annotation("SqlServer:Include", new[] { "Locus", "MatchConfidence_1", "MatchConfidence_2", "IsAntigenMatch_1", "IsAntigenMatch_2" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id",
                table: "LocusMatchDetails");

            migrationBuilder.CreateIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount",
                table: "LocusMatchDetails",
                columns: new[] { "MatchedDonor_Id", "Locus", "MatchCount" });
        }
    }
}
