using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNomenclatureVersionToSubjectGenotypeSetValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey",
                table: "SubjectGenotypeSetValues");

            migrationBuilder.AddColumn<string>(
                name: "MatchingAlgorithmHlaNomenclatureVersion",
                table: "SubjectGenotypeSetValues",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_MatchingAlgorithmHlaNomenclatureVersion_AllowedLociKey",
                table: "SubjectGenotypeSetValues",
                columns: new[] { "HlaTypingKey", "HaplotypeFrequencySetId", "MatchingAlgorithmHlaNomenclatureVersion", "AllowedLociKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_MatchingAlgorithmHlaNomenclatureVersion_AllowedLociKey",
                table: "SubjectGenotypeSetValues");

            migrationBuilder.DropColumn(
                name: "MatchingAlgorithmHlaNomenclatureVersion",
                table: "SubjectGenotypeSetValues");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey",
                table: "SubjectGenotypeSetValues",
                columns: new[] { "HlaTypingKey", "HaplotypeFrequencySetId", "AllowedLociKey" },
                unique: true);
        }
    }
}
