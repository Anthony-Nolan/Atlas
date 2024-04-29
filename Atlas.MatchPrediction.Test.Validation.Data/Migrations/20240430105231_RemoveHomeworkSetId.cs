using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class RemoveHomeworkSetId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonorImputationSummaries_HomeworkSets_HomeworkSet_Id",
                table: "DonorImputationSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientImputationSummaries_HomeworkSets_HomeworkSet_Id",
                table: "PatientImputationSummaries");

            migrationBuilder.DropIndex(
                name: "IX_PatientImputationSummaries_ExternalSubjectId_HomeworkSet_Id",
                table: "PatientImputationSummaries");

            migrationBuilder.DropIndex(
                name: "IX_PatientImputationSummaries_HomeworkSet_Id",
                table: "PatientImputationSummaries");

            migrationBuilder.DropIndex(
                name: "IX_DonorImputationSummaries_ExternalSubjectId_HomeworkSet_Id",
                table: "DonorImputationSummaries");

            migrationBuilder.DropIndex(
                name: "IX_DonorImputationSummaries_HomeworkSet_Id",
                table: "DonorImputationSummaries");

            migrationBuilder.DropColumn(
                name: "HomeworkSet_Id",
                table: "PatientImputationSummaries");

            migrationBuilder.DropColumn(
                name: "HomeworkSet_Id",
                table: "DonorImputationSummaries");

            migrationBuilder.CreateIndex(
                name: "IX_PatientImputationSummaries_ExternalSubjectId",
                table: "PatientImputationSummaries",
                column: "ExternalSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DonorImputationSummaries_ExternalSubjectId",
                table: "DonorImputationSummaries",
                column: "ExternalSubjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientImputationSummaries_ExternalSubjectId",
                table: "PatientImputationSummaries");

            migrationBuilder.DropIndex(
                name: "IX_DonorImputationSummaries_ExternalSubjectId",
                table: "DonorImputationSummaries");

            migrationBuilder.AddColumn<int>(
                name: "HomeworkSet_Id",
                table: "PatientImputationSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HomeworkSet_Id",
                table: "DonorImputationSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PatientImputationSummaries_ExternalSubjectId_HomeworkSet_Id",
                table: "PatientImputationSummaries",
                columns: new[] { "ExternalSubjectId", "HomeworkSet_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientImputationSummaries_HomeworkSet_Id",
                table: "PatientImputationSummaries",
                column: "HomeworkSet_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DonorImputationSummaries_ExternalSubjectId_HomeworkSet_Id",
                table: "DonorImputationSummaries",
                columns: new[] { "ExternalSubjectId", "HomeworkSet_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DonorImputationSummaries_HomeworkSet_Id",
                table: "DonorImputationSummaries",
                column: "HomeworkSet_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DonorImputationSummaries_HomeworkSets_HomeworkSet_Id",
                table: "DonorImputationSummaries",
                column: "HomeworkSet_Id",
                principalTable: "HomeworkSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientImputationSummaries_HomeworkSets_HomeworkSet_Id",
                table: "PatientImputationSummaries",
                column: "HomeworkSet_Id",
                principalTable: "HomeworkSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
