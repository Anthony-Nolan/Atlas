using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.RepeatSearch.Data.Migrations
{
    /// <summary>
    /// Replaces the unique index of SearchResults on (ExternalDonorCode, CanonicalResultSetId) by one on (CanonicalResultSetId, ExternalDonorCode),
    /// and drops the index on CanonicalResultSetId, which the new index makes redundant.
    /// With the donor code first, the rows of a new set go to random pages of the index, which makes inserts slow on a large table (ATL-568).
    /// The new index is built online and before the old ones are dropped, so the table always has a unique index on the two columns and an index
    /// that starts with the set id, and the apps can keep working during the build.
    /// Each step checks whether it is already done, so that a large installation can build the new index by hand before the release
    /// (see the 4.2.0 entry of CHANGELOG_Data.md).
    /// </summary>
    public partial class SearchResultsIndexSetIdFirst : Migration
    {
        private const string NewIndex = "IX_SearchResults_CanonicalResultSetId_ExternalDonorCode";
        private const string OldUniqueIndex = "IX_SearchResults_ExternalDonorCode_CanonicalResultSetId";
        private const string OldSetIdIndex = "IX_SearchResults_CanonicalResultSetId";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateIndexIfMissing(NewIndex, "[CanonicalResultSetId], [ExternalDonorCode]", unique: true));
            migrationBuilder.Sql(DropIndexIfExists(OldUniqueIndex));
            migrationBuilder.Sql(DropIndexIfExists(OldSetIdIndex));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateIndexIfMissing(OldSetIdIndex, "[CanonicalResultSetId]", unique: false));
            migrationBuilder.Sql(CreateIndexIfMissing(OldUniqueIndex, "[ExternalDonorCode], [CanonicalResultSetId]", unique: true));
            migrationBuilder.Sql(DropIndexIfExists(NewIndex));
        }

        private static string CreateIndexIfMissing(string name, string columns, bool unique) => $@"
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{name}' AND object_id = OBJECT_ID('RepeatSearch.SearchResults'))
            BEGIN
                CREATE {(unique ? "UNIQUE " : "")}INDEX [{name}] ON [RepeatSearch].[SearchResults] ({columns}) WITH (ONLINE = ON);
            END";

        private static string DropIndexIfExists(string name) => $@"
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{name}' AND object_id = OBJECT_ID('RepeatSearch.SearchResults'))
            BEGIN
                DROP INDEX [{name}] ON [RepeatSearch].[SearchResults];
            END";
    }
}
