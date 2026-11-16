using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.RepeatSearch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalResultSetStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing sets were stored in one transaction, so they are all complete (CanonicalResultSetStatus.Complete = 1).
            // A constant default makes this a metadata-only change, with no update of existing rows.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreAttemptId",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "RepeatSearch",
                table: "CanonicalResultSets");

            migrationBuilder.DropColumn(
                name: "StoreAttemptId",
                schema: "RepeatSearch",
                table: "CanonicalResultSets");
        }
    }
}
