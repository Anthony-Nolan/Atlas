using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class CombinePatientAndDonorTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorGenotypes");

            migrationBuilder.DropTable(
                name: "PatientGenotypes");

            migrationBuilder.DropTable(
                name: "DonorImputationSummaries");

            migrationBuilder.DropTable(
                name: "PatientImputationSummaries");

            migrationBuilder.CreateTable(
                name: "ImputationSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalSubjectId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    HfSetPopulationId = table.Column<int>(type: "int", nullable: false),
                    WasRepresented = table.Column<bool>(type: "bit", nullable: false),
                    GenotypeCount = table.Column<int>(type: "int", nullable: false),
                    SumOfLikelihoods = table.Column<decimal>(type: "decimal(21,20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImputationSummaries", x => x.Id);
                });

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
                    Likelihood = table.Column<decimal>(type: "decimal(21,20)", nullable: true),
                    ImputationSummary_Id = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_ImputationSummaries_ExternalSubjectId",
                table: "ImputationSummaries",
                column: "ExternalSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGenotypes_ImputationSummary_Id",
                table: "SubjectGenotypes",
                column: "ImputationSummary_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectGenotypes");

            migrationBuilder.DropTable(
                name: "ImputationSummaries");

            migrationBuilder.CreateTable(
                name: "DonorImputationSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalSubjectId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    GenotypeCount = table.Column<int>(type: "int", nullable: false),
                    HfSetPopulationId = table.Column<int>(type: "int", nullable: false),
                    SumOfLikelihoods = table.Column<decimal>(type: "decimal(21,20)", nullable: false),
                    WasRepresented = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorImputationSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientImputationSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalSubjectId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    GenotypeCount = table.Column<int>(type: "int", nullable: false),
                    HfSetPopulationId = table.Column<int>(type: "int", nullable: false),
                    SumOfLikelihoods = table.Column<decimal>(type: "decimal(21,20)", nullable: false),
                    WasRepresented = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientImputationSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonorGenotypes",
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
                    table.PrimaryKey("PK_DonorGenotypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonorGenotypes_DonorImputationSummaries_ImputationSummary_Id",
                        column: x => x.ImputationSummary_Id,
                        principalTable: "DonorImputationSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientGenotypes",
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
                    table.PrimaryKey("PK_PatientGenotypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientGenotypes_PatientImputationSummaries_ImputationSummary_Id",
                        column: x => x.ImputationSummary_Id,
                        principalTable: "PatientImputationSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorGenotypes_ImputationSummary_Id",
                table: "DonorGenotypes",
                column: "ImputationSummary_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DonorImputationSummaries_ExternalSubjectId",
                table: "DonorImputationSummaries",
                column: "ExternalSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientGenotypes_ImputationSummary_Id",
                table: "PatientGenotypes",
                column: "ImputationSummary_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PatientImputationSummaries_ExternalSubjectId",
                table: "PatientImputationSummaries",
                column: "ExternalSubjectId");
        }
    }
}
