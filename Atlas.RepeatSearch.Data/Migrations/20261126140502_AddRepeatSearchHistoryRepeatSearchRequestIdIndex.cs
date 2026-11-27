using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.RepeatSearch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepeatSearchHistoryRepeatSearchRequestIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RepeatSearchHistoryRecords_RepeatSearchRequestId",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords",
                column: "RepeatSearchRequestId")
                .Annotation("SqlServer:Online", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepeatSearchHistoryRecords_RepeatSearchRequestId",
                schema: "RepeatSearch",
                table: "RepeatSearchHistoryRecords");
        }
    }
}
