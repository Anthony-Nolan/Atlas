using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddDataRefreshColumnsWithReordering_Indexes_Continue_Comments : Migration
    {
        /// <remarks>
        /// Yeesh!
        /// There are 2 sets of changes here - making a column Non-Nullable, and adding some new columns to this table.
        /// Unfortunately we're very invested in the order of the columns of this table, and the new columns need to go in the middle.
        /// So we need to take all the columns out, and put them all back in again, in order.
        ///
        /// In the meantime we'll store a copy of the data elsewhere, remove it from the table in question and then re-instate it at the end.
        /// Fortunately there are no ForeignKeys, so we don't need to deal with those!
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var originalTable = "DataRefreshHistory";
            var cloneTable = "temporary_DataRefreshHistory_Clone";

            CloneExistingData(migrationBuilder, cloneTable, originalTable);
            DeleteExistingData(migrationBuilder, originalTable);
            RemoveAllExistingColumns(migrationBuilder, originalTable);
            ReAddAllColumns_IncludingNewColumns_InOrder(migrationBuilder, originalTable);
            CopyClonedDataBackIntoTable(migrationBuilder, originalTable, cloneTable);
            ResetIdColumn(migrationBuilder, originalTable);
            DeleteCloneTable(migrationBuilder, cloneTable);
        }

        #region UpMigrationSteps
        private static void CloneExistingData(MigrationBuilder migrationBuilder, string cloneTable, string originalTable)
        {
            migrationBuilder.Sql($"SELECT * INTO {cloneTable} FROM {originalTable}");
        }

        private static void DeleteExistingData(MigrationBuilder migrationBuilder, string originalTable)
        {
            migrationBuilder.Sql($"DELETE FROM {originalTable}");
        }

        private static void RemoveAllExistingColumns(MigrationBuilder migrationBuilder, string originalTable)
        {
            //Leave Id behind, since it should be first, isn't changing and is most complex.
            migrationBuilder.DropColumn(name: "RefreshBeginUtc", table: originalTable);
            migrationBuilder.DropColumn(name: "RefreshEndUtc", table: originalTable);
            migrationBuilder.DropColumn(name: "Database", table: originalTable);
            migrationBuilder.DropColumn(name: "HlaNomenclatureVersion", table: originalTable);
            migrationBuilder.DropColumn(name: "WasSuccessful", table: originalTable);
            migrationBuilder.DropColumn(name: "DataDeletionCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "DatabaseScalingSetupCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "DatabaseScalingTearDownCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "DonorHlaProcessingCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "DonorImportCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "MetadataDictionaryRefreshCompleted", table: originalTable);
            migrationBuilder.DropColumn(name: "QueuedDonorUpdatesCompleted", table: originalTable);
        }

        private static void ReAddAllColumns_IncludingNewColumns_InOrder(MigrationBuilder migrationBuilder, string originalTable)
        {
            //Id already in place
            migrationBuilder.AddColumn<string>(name: "Database", table: originalTable, nullable: false); // Note nullability has changed!
            migrationBuilder.AddColumn<bool>(name: "WasSuccessful", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SupportComments", table: originalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "RefreshBeginUtc", table: originalTable, nullable: false);
            migrationBuilder.AddColumn<DateTime>(name: "RefreshContinueUtc", table: originalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "RefreshEndUtc", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HlaNomenclatureVersion", table: originalTable, nullable: true);  //Moved!
            migrationBuilder.AddColumn<DateTime>(name: "MetadataDictionaryRefreshCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "IndexDeletionCompleted", table: originalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "DataDeletionCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DatabaseScalingSetupCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DonorImportCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DonorHlaProcessingCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "IndexRecreationCompleted", table: originalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "DatabaseScalingTearDownCompleted", table: originalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "QueuedDonorUpdatesCompleted", table: originalTable, nullable: true);
        }

        private static void CopyClonedDataBackIntoTable(MigrationBuilder migrationBuilder, string originalTable,
            string cloneTable)
        {
            var currentColumns = @"
             [Id]
            ,[RefreshBeginUtc]
            ,[RefreshEndUtc]
            ,[Database]
            ,[HlaNomenclatureVersion]
            ,[WasSuccessful]
            ,[MetadataDictionaryRefreshCompleted]
            ,[DataDeletionCompleted]
            ,[DatabaseScalingSetupCompleted]
            ,[DonorImportCompleted]
            ,[DonorHlaProcessingCompleted]
            ,[DatabaseScalingTearDownCompleted]
            ,[QueuedDonorUpdatesCompleted]
";

            migrationBuilder.Sql($@"
SET IDENTITY_INSERT {originalTable} ON

INSERT INTO {originalTable}(
  {currentColumns}
)
SELECT
  {currentColumns}
FROM {cloneTable}

SET IDENTITY_INSERT {originalTable} OFF");
        }

        private static void ResetIdColumn(MigrationBuilder migrationBuilder, string originalTable)
        {
            migrationBuilder.Sql($@"
DECLARE @max int = (SELECT COALESCE(MAX(Id), 1) FROM {originalTable});
DBCC CHECKIDENT({originalTable}, RESEED, @max)"
            );
        }

        private static void DeleteCloneTable(MigrationBuilder migrationBuilder, string cloneTable)
        {
            migrationBuilder.Sql($"DROP TABLE {cloneTable}");
        }
        #endregion

        /// <remarks>
        /// Whilst the up-migration is very complex the down migration doesn't need to be, because we don't care about column order on the way down.
        /// A) The re-ordering above is idempotent, so we don't NEED to reset it precisely
        /// B) Mostly this is removing columns so it'll end up in almost exactly the same order anyway.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndexDeletionCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "IndexRecreationCompleted",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "RefreshContinueUtc",
                table: "DataRefreshHistory");

            migrationBuilder.DropColumn(
                name: "SupportComments",
                table: "DataRefreshHistory");

            migrationBuilder.AlterColumn<string>(
                name: "Database",
                table: "DataRefreshHistory",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
