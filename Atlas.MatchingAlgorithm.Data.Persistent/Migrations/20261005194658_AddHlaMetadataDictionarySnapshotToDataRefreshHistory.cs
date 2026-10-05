using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class AddHlaMetadataDictionarySnapshotToDataRefreshHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HlaMetadataDictionarySnapshotUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HlaMetadataDictionarySnapshotUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
