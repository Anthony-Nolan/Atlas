using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.DonorImport.Data.Migrations
{
    public partial class AddNewColumnsToPublishableDonorUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedOn",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates",
                column: "IsPublished",
                filter: "[IsPublished] = 0")
                .Annotation("SqlServer:Include", new[] { "SearchableDonorUpdate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublishableDonorUpdates_IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                schema: "Donors",
                table: "PublishableDonorUpdates");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Donors",
                table: "PublishableDonorUpdates");

            migrationBuilder.DropColumn(
                name: "PublishedOn",
                schema: "Donors",
                table: "PublishableDonorUpdates");
        }
    }
}
