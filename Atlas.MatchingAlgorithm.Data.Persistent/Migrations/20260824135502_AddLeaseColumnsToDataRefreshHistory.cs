using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseColumnsToDataRefreshHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseOwner",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeaseExpiresUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
