using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class NewIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorCode_TotalMatchCount",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount",
                table: "LocusMatchDetails");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSets_DonorType",
                table: "SearchSets",
                column: "DonorType");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_WasSuccessful",
                table: "SearchRequests",
                column: "WasSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id",
                table: "MatchedDonors",
                column: "SearchRequestRecord_Id")
                .Annotation("SqlServer:Include", new[] { "DonorCode", "TotalMatchCount", "PatientHfSetPopulationId", "DonorHfSetPopulationId", "WasPatientRepresented", "WasDonorRepresented" });

            migrationBuilder.CreateIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id",
                table: "LocusMatchDetails",
                column: "MatchedDonor_Id")
                .Annotation("SqlServer:Include", new[] { "Locus", "MatchConfidence_1", "MatchConfidence_2", "IsAntigenMatch_1", "IsAntigenMatch_2" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchSets_DonorType",
                table: "SearchSets");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_WasSuccessful",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id",
                table: "LocusMatchDetails");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorCode_TotalMatchCount",
                table: "MatchedDonors",
                columns: new[] { "SearchRequestRecord_Id", "DonorCode", "TotalMatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_LocusMatchDetails_MatchedDonor_Id_Locus_MatchCount",
                table: "LocusMatchDetails",
                columns: new[] { "MatchedDonor_Id", "Locus", "MatchCount" });
        }
    }
}
