using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.RepeatSearch.Data.Migrations
{
    /// <summary>
    /// Adds LastUsedAt to CanonicalResultSets, for the retention job (ATL-550), and fills it for the existing sets:
    /// the later of the date of the original search (search tracking) and the date of the last repeat search (repeat search history).
    /// A set with no search tracking row, but stored after search tracking started (for example after a lost tracking event), gets the
    /// latest request time of the tracked sets with a lower Id instead: set ids grow with time, so this is an estimate of when it was stored.
    /// If no date is known, for a set stored before search tracking started, the date search tracking started, so that the set counts as old.
    /// If there is no search tracking date at all, the set keeps the column default, the time of this migration.
    /// The column default is a runtime constant, so adding the column does not write the existing rows.
    /// The index is created after the update, so that the update does not also write the index.
    /// </summary>
    public partial class AddCanonicalResultSetLastUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAt",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            // The SearchTracking schema is in the same database, but a new database can have no search tracking tables yet: its sets
            // table is empty then. The statements that read search tracking are dynamic SQL, so that they are compiled only if the table exists.
            // RequestTimeUtc is datetime2 in UTC: it converts to datetimeoffset with offset +00:00.
            // SearchIdentifier is nvarchar(36) with lowercase GUIDs. The join compares text and converts only the set side, so it can seek
            // on IX_SearchIdentifier, and an id that is not a GUID matches nothing instead of failing the conversion.
            migrationBuilder.Sql(@"
                DECLARE @searchTrackingStart datetimeoffset;

                IF OBJECT_ID(N'SearchTracking.SearchRequests', N'U') IS NOT NULL
                    EXEC sp_executesql
                        N'SELECT @start = MIN(RequestTimeUtc) FROM SearchTracking.SearchRequests',
                        N'@start datetimeoffset OUTPUT',
                        @start = @searchTrackingStart OUTPUT;

                UPDATE crs
                SET LastUsedAt = COALESCE(history.LastRepeatSearch, @searchTrackingStart, crs.LastUsedAt)
                FROM RepeatSearch.CanonicalResultSets crs
                LEFT JOIN (
                    SELECT OriginalSearchRequestId, MAX(DateCreated) AS LastRepeatSearch
                    FROM RepeatSearch.RepeatSearchHistoryRecords
                    GROUP BY OriginalSearchRequestId
                ) history ON history.OriginalSearchRequestId = crs.OriginalSearchRequestId;

                IF OBJECT_ID(N'SearchTracking.SearchRequests', N'U') IS NOT NULL
                    EXEC sp_executesql N'
                        WITH sets AS (
                            SELECT crs.LastUsedAt,
                                   searchRequest.RequestTimeUtc,
                                   MAX(searchRequest.RequestTimeUtc) OVER (
                                       ORDER BY crs.Id ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) AS EarlierRequestTimeUtc
                            FROM RepeatSearch.CanonicalResultSets crs
                            LEFT JOIN SearchTracking.SearchRequests searchRequest
                                ON searchRequest.SearchIdentifier = LOWER(crs.OriginalSearchRequestId)
                        )
                        UPDATE sets
                        SET LastUsedAt = COALESCE(RequestTimeUtc, EarlierRequestTimeUtc)
                        WHERE COALESCE(RequestTimeUtc, EarlierRequestTimeUtc) > LastUsedAt';");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalResultSets_LastUsedAt",
                schema: "RepeatSearch",
                table: "CanonicalResultSets",
                column: "LastUsedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CanonicalResultSets_LastUsedAt",
                schema: "RepeatSearch",
                table: "CanonicalResultSets");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                schema: "RepeatSearch",
                table: "CanonicalResultSets");
        }
    }
}
