using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class RemovePerformanceChangeToDonorCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchPredictionTimeInMs",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "MatchingAlgorithmTimeInMs",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "OverallSearchTimeInMs",
                table: "SearchRequests");


            migrationBuilder.DropForeignKey(
                name: "FK_MatchedDonors_Simulants_DonorId",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_DonorId",
                table: "MatchedDonors");

            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorId_TotalMatchCount",
                table: "MatchedDonors");

            migrationBuilder.AlterColumn<string>(
                name: "DonorId",
                table: "MatchedDonors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false);

            migrationBuilder.RenameColumn(
                name: "DonorId",
                table: "MatchedDonors",
                newName: "DonorCode");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorCode_TotalMatchCount",
                table: "MatchedDonors",
                columns: new[] { "SearchRequestRecord_Id", "DonorCode", "TotalMatchCount" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorCode_TotalMatchCount",
                table: "MatchedDonors");

            migrationBuilder.AlterColumn<int>(
                name: "DonorCode",
                table: "MatchedDonors",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldNullable: false);

            migrationBuilder.RenameColumn(
                name: "DonorCode",
                table: "MatchedDonors",
                newName: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_DonorId",
                table: "MatchedDonors",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedDonors_SearchRequestRecord_Id_DonorId_TotalMatchCount",
                table: "MatchedDonors",
                columns: new[] { "SearchRequestRecord_Id", "DonorId", "TotalMatchCount" });

            migrationBuilder.AddForeignKey(
                name: "FK_MatchedDonors_Simulants_DonorId",
                table: "MatchedDonors",
                column: "DonorId",
                principalTable: "Simulants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);


            migrationBuilder.AddColumn<double>(
                name: "MatchPredictionTimeInMs",
                table: "SearchRequests",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchingAlgorithmTimeInMs",
                table: "SearchRequests",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OverallSearchTimeInMs",
                table: "SearchRequests",
                type: "float",
                nullable: true);
        }
    }
}
