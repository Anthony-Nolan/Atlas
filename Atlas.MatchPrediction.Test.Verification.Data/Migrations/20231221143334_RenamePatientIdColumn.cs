using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RenamePatientIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchRequests_Simulants_PatientSimulant_Id",
                table: "SearchRequests");

            migrationBuilder.RenameColumn(
                name: "PatientSimulant_Id",
                table: "SearchRequests",
                newName: "PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequests_VerificationRun_Id_PatientSimulant_Id_SearchResultsRetrieved",
                table: "SearchRequests",
                newName: "IX_SearchRequests_VerificationRun_Id_PatientId_SearchResultsRetrieved");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequests_PatientSimulant_Id",
                table: "SearchRequests",
                newName: "IX_SearchRequests_PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchRequests_Simulants_PatientId",
                table: "SearchRequests",
                column: "PatientId",
                principalTable: "Simulants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchRequests_Simulants_PatientId",
                table: "SearchRequests");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "SearchRequests",
                newName: "PatientSimulant_Id");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequests_VerificationRun_Id_PatientId_SearchResultsRetrieved",
                table: "SearchRequests",
                newName: "IX_SearchRequests_VerificationRun_Id_PatientSimulant_Id_SearchResultsRetrieved");

            migrationBuilder.RenameIndex(
                name: "IX_SearchRequests_PatientId",
                table: "SearchRequests",
                newName: "IX_SearchRequests_PatientSimulant_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchRequests_Simulants_PatientSimulant_Id",
                table: "SearchRequests",
                column: "PatientSimulant_Id",
                principalTable: "Simulants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
