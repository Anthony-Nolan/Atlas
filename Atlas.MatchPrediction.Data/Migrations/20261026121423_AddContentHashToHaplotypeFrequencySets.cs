using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentHashToHaplotypeFrequencySets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ContentHash",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencySets",
                type: "binary(32)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencySets");
        }
    }
}
