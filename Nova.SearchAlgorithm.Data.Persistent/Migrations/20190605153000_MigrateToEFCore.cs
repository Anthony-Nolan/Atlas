using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.SearchAlgorithm.Data.Persistent.Core.Migrations
{
    public partial class MigrateToEFCore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfidenceWeightings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    Weight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidenceWeightings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataRefreshHistory",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RefreshBeginUtc = table.Column<DateTime>(nullable: false),
                    RefreshEndUtc = table.Column<DateTime>(nullable: true),
                    Database = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRefreshHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeWeightings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    Weight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeWeightings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfidenceWeightings",
                columns: new[] { "Id", "Name", "Weight" },
                values: new object[,]
                {
                    { 1, "Mismatch", 0 },
                    { 2, "PermissiveMismatch", 0 },
                    { 3, "Broad", 0 },
                    { 4, "Split", 0 }
                });

            migrationBuilder.InsertData(
                table: "GradeWeightings",
                columns: new[] { "Id", "Name", "Weight" },
                values: new object[,]
                {
                    { 12, "Protein", 0 },
                    { 11, "GGroup", 0 },
                    { 10, "PGroup", 0 },
                    { 9, "NullGDna", 0 },
                    { 8, "NullCDna", 0 },
                    { 7, "NullPartial", 0 },
                    { 5, "Associated", 0 },
                    { 13, "CDna", 0 },
                    { 4, "Split", 0 },
                    { 3, "Broad", 0 },
                    { 2, "PermissiveMismatch", 0 },
                    { 1, "Mismatch", 0 },
                    { 6, "NullMismatch", 0 },
                    { 14, "GDna", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidenceWeightings_Name",
                table: "ConfidenceWeightings",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GradeWeightings_Name",
                table: "GradeWeightings",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfidenceWeightings");

            migrationBuilder.DropTable(
                name: "DataRefreshHistory");

            migrationBuilder.DropTable(
                name: "GradeWeightings");
        }
    }
}
