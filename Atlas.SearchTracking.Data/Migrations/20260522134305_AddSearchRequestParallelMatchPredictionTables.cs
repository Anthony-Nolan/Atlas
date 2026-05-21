using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchRequestParallelMatchPredictionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchRequestParallelMatchPredictionMetadata",
                schema: "SearchTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchIdentifier = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRepeatSearch = table.Column<bool>(type: "bit", nullable: false),
                    RepeatSearchIdentifier = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultsFileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResultsBatched = table.Column<bool>(type: "bit", nullable: false),
                    BatchFolderName = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    MatchingAlgorithmElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                    SearchInitiatedTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalBatchCount = table.Column<int>(type: "int", nullable: false),
                    ProcessedBatchCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequestParallelMatchPredictionMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchRequestParallelMatchPredictionResultLocations",
                schema: "SearchTracking",
                columns: table => new
                {
                    MetadataId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ResultBlobFileName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchRequestParallelMatchPredictionResultLocations", x => new { x.MetadataId, x.DonorId });
                    table.ForeignKey(
                        name: "FK_SearchRequestParallelMatchPredictionResultLocations_SearchRequestParallelMatchPredictionMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalSchema: "SearchTracking",
                        principalTable: "SearchRequestParallelMatchPredictionMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchRequestParallelMatchPredictionResultLocations",
                schema: "SearchTracking");

            migrationBuilder.DropTable(
                name: "SearchRequestParallelMatchPredictionMetadata",
                schema: "SearchTracking");
        }
    }
}
