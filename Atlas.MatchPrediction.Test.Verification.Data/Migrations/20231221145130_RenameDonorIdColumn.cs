using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RenameDonorIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchedDonors_Simulants_MatchedDonorSimulant_Id",
                table: "MatchedDonors");

            migrationBuilder.RenameColumn(
                name: "MatchedDonorSimulant_Id",
                table: "MatchedDonors",
                newName: "DonorId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_MatchedDonorSimulant_Id_TotalMatchCount",
                table: "MatchedDonors",
                newName: "IX_MatchedDonors_SearchRequestRecord_Id_DonorId_TotalMatchCount");

            migrationBuilder.RenameIndex(
                name: "IX_MatchedDonors_MatchedDonorSimulant_Id",
                table: "MatchedDonors",
                newName: "IX_MatchedDonors_DonorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchedDonors_Simulants_DonorId",
                table: "MatchedDonors",
                column: "DonorId",
                principalTable: "Simulants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchedDonors_Simulants_DonorId",
                table: "MatchedDonors");

            migrationBuilder.RenameColumn(
                name: "DonorId",
                table: "MatchedDonors",
                newName: "MatchedDonorSimulant_Id");

            migrationBuilder.RenameIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorId_TotalMatchCount",
                table: "MatchedDonors",
                newName: "IX_MatchedDonors_SearchRequestRecord_Id_MatchedDonorSimulant_Id_TotalMatchCount");

            migrationBuilder.RenameIndex(
                name: "IX_MatchedDonors_DonorId",
                table: "MatchedDonors",
                newName: "IX_MatchedDonors_MatchedDonorSimulant_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchedDonors_Simulants_MatchedDonorSimulant_Id",
                table: "MatchedDonors",
                column: "MatchedDonorSimulant_Id",
                principalTable: "Simulants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
