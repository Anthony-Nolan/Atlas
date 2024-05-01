using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class MatchingGenotypesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectGenotypes");

            migrationBuilder.CreateTable(
                name: "MatchingGenotypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    A_Count = table.Column<int>(type: "int", nullable: false),
                    B_Count = table.Column<int>(type: "int", nullable: false),
                    C_Count = table.Column<int>(type: "int", nullable: true),
                    DQB1_Count = table.Column<int>(type: "int", nullable: true),
                    DRB1_Count = table.Column<int>(type: "int", nullable: false),
                    Patient_A_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_A_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_B_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_B_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_C_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Patient_C_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Patient_DQB1_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Patient_DQB1_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Patient_DRB1_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_DRB1_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Patient_Likelihood = table.Column<decimal>(type: "decimal(21,20)", nullable: false),
                    Donor_A_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_A_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_B_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_B_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_C_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Donor_C_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Donor_DQB1_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Donor_DQB1_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Donor_DRB1_1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_DRB1_2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Donor_Likelihood = table.Column<decimal>(type: "decimal(21,20)", nullable: false),
                    Patient_ImputationSummary_Id = table.Column<int>(type: "int", nullable: false),
                    Donor_ImputationSummary_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingGenotypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchingGenotypes_ImputationSummaries_Donor_ImputationSummary_Id",
                        column: x => x.Donor_ImputationSummary_Id,
                        principalTable: "ImputationSummaries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchingGenotypes_ImputationSummaries_Patient_ImputationSummary_Id",
                        column: x => x.Patient_ImputationSummary_Id,
                        principalTable: "ImputationSummaries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchingGenotypes_Donor_ImputationSummary_Id",
                table: "MatchingGenotypes",
                column: "Donor_ImputationSummary_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingGenotypes_Patient_ImputationSummary_Id",
                table: "MatchingGenotypes",
                column: "Patient_ImputationSummary_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingGenotypes_TotalCount",
                table: "MatchingGenotypes",
                column: "TotalCount");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchingGenotypes");

            migrationBuilder.CreateTable(
                name: "SubjectGenotypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    A_1 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    A_2 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    B_1 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    B_2 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    C_1 = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    C_2 = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    DQB1_1 = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    DQB1_2 = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    DRB1_1 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    DRB1_2 = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    ImputationSummary_Id = table.Column<int>(type: "int", nullable: false),
                    Likelihood = table.Column<decimal>(type: "decimal(21,20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGenotypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectGenotypes_ImputationSummaries_ImputationSummary_Id",
                        column: x => x.ImputationSummary_Id,
                        principalTable: "ImputationSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGenotypes_ImputationSummary_Id",
                table: "SubjectGenotypes",
                column: "ImputationSummary_Id");
        }
    }
}
