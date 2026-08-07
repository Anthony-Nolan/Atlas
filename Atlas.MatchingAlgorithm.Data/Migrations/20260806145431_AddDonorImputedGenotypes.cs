using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorImputedGenotypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorImputedGenotypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    AllowedLociKey = table.Column<int>(type: "int", nullable: false),
                    IsUnrepresented = table.Column<bool>(type: "bit", nullable: false),
                    ImputedGenotypeData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HaplotypeFrequencySetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorImputedGenotypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorImputedGenotypes_DonorId_AllowedLociKey",
                table: "DonorImputedGenotypes",
                columns: new[] { "DonorId", "AllowedLociKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorImputedGenotypes");
        }
    }
}
