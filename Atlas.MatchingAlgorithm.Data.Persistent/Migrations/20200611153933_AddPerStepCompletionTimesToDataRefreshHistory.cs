using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddPerStepCompletionTimesToDataRefreshHistory : Migration
    {
        /// <summary>
        /// Columns in this migration have been re-ordered from the EF generated migration, which added them alphabetically.
        /// These columns can not be re-ordered once this migration has run - see https://github.com/dotnet/efcore/issues/10059
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MetadataDictionaryRefreshCompleted",
                table: "DataRefreshHistory",
                nullable: true);
            
            migrationBuilder.AddColumn<DateTime>(
                name: "DataDeletionCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatabaseScalingSetupCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DonorImportCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DonorHlaProcessingCompleted",
                table: "DataRefreshHistory",
                nullable: true);
            
            migrationBuilder.AddColumn<DateTime>(
                name: "DatabaseScalingTearDownCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueuedDonorUpdatesCompleted",
                table: "DataRefreshHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataDeletionCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "DatabaseScalingSetupCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "DatabaseScalingTearDownCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "DonorHlaProcessingCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "DonorImportCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "MetadataDictionaryRefreshCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "QueuedDonorUpdatesCompleted",
                table: "DataRefreshHistory");
        }
    }
}
