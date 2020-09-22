using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class ModifyTableDataRefreshHistory_ModifyContinueColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshBeginUtc",
                newName: "RefreshRequestedUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.RenameColumn(
                name: "RefreshContinueUtc",
                newName: "RefreshLastContinuedUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.AddColumn<int>(
                name: "RefreshContinuedCount",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshContinuedCount",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.RenameColumn(
                name: "RefreshRequestedUtc",
                newName: "RefreshBeginUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");

            migrationBuilder.RenameColumn(
                name: "RefreshLastContinuedUtc",
                newName: "RefreshContinueUtc",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
