using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddPerStepCompletionTimesToDataRefreshHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataDeletionCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatabaseScalingSetupCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatabaseScalingTearDownCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DonorHlaProcessingCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DonorImportCompleted",
                table: "DataRefreshHistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MetadataDictionaryRefreshCompleted",
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
