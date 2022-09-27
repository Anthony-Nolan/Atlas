using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    public partial class InitialiseDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    A_1 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    A_2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    B_1 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    B_2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    C_1 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    C_2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DQB1_1 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DQB1_2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DRB1_1 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DRB1_2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchPredictionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    MatchPredictionAlgorithmRequestId = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPredictionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPredictionRequests_SubjectInfo_DonorId",
                        column: x => x.DonorId,
                        principalTable: "SubjectInfo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchPredictionRequests_SubjectInfo_PatientId",
                        column: x => x.PatientId,
                        principalTable: "SubjectInfo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatchPredictionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchPredictionRequestId = table.Column<int>(type: "int", nullable: false),
                    Locus = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(6,5)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPredictionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPredictionResults_MatchPredictionRequests_MatchPredictionRequestId",
                        column: x => x.MatchPredictionRequestId,
                        principalTable: "MatchPredictionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPredictionRequests_DonorId",
                table: "MatchPredictionRequests",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPredictionRequests_MatchPredictionAlgorithmRequestId_DonorId_PatientId",
                table: "MatchPredictionRequests",
                columns: new[] { "MatchPredictionAlgorithmRequestId", "DonorId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPredictionRequests_PatientId",
                table: "MatchPredictionRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPredictionResults_MatchPredictionRequestId_Locus_MismatchCount",
                table: "MatchPredictionResults",
                columns: new[] { "MatchPredictionRequestId", "Locus", "MismatchCount" });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectInfo_ExternalId",
                table: "SubjectInfo",
                column: "ExternalId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPredictionResults");

            migrationBuilder.DropTable(
                name: "MatchPredictionRequests");

            migrationBuilder.DropTable(
                name: "SubjectInfo");
        }
    }
}
