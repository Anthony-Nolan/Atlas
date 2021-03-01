using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.RepeatSearch.Data.Migrations
{
    public partial class AddRepeatSearchHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepeatSearchHistoryRecords",
                schema: "RepeatSearch",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalSearchRequestId = table.Column<string>(maxLength: 200, nullable: false),
                    RepeatSearchRequestId = table.Column<string>(maxLength: 200, nullable: false),
                    SearchCutoffDate = table.Column<DateTimeOffset>(nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepeatSearchHistoryRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepeatSearchHistoryRecords",
                schema: "RepeatSearch");
        }
    }
}
