using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorSubjectGenotypeSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorSubjectGenotypeSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    AllowedLociKey = table.Column<int>(type: "int", nullable: false),
                    SubjectGenotypeSetValueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorSubjectGenotypeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubjectGenotypeSetValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaTypingKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AllowedLociKey = table.Column<int>(type: "int", nullable: false),
                    HaplotypeFrequencySetId = table.Column<int>(type: "int", nullable: false),
                    MatchingAlgorithmHlaNomenclatureVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsUnrepresented = table.Column<bool>(type: "bit", nullable: false),
                    SubjectGenotypeSetData = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGenotypeSetValues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey",
                table: "DonorSubjectGenotypeSets",
                columns: new[] { "DonorId", "AllowedLociKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_MatchingAlgorithmHlaNomenclatureVersion_AllowedLociKey",
                table: "SubjectGenotypeSetValues",
                columns: new[] { "HlaTypingKey", "HaplotypeFrequencySetId", "MatchingAlgorithmHlaNomenclatureVersion", "AllowedLociKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorSubjectGenotypeSets");

            migrationBuilder.DropTable(
                name: "SubjectGenotypeSetValues");
        }
    }
}
