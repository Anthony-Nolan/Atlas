using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    public partial class AddDataRefreshColumnsWithReordering_Indexes_Continue_Comments : Migration
    {
        private const string OriginalTable = "DataRefreshHistory";
        private const string CloneTable = "temporary_DataRefreshHistory_Clone";

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
            CloneExistingData(migrationBuilder);
            DeleteExistingData(migrationBuilder);
            RemoveAllExistingColumns(migrationBuilder);
            ReAddAllColumns_IncludingNewColumns_InOrder(migrationBuilder);
            CopyClonedDataBackIntoTable(migrationBuilder);
            ResetIdColumn(migrationBuilder);
            DeleteCloneTable(migrationBuilder);
        }

        #region UpMigrationSteps
        private static void CloneExistingData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"SELECT * INTO {CloneTable} FROM {OriginalTable}");
        }

        private static void DeleteExistingData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM {OriginalTable}");
        }

        private static void RemoveAllExistingColumns(MigrationBuilder migrationBuilder)
        {
            //Leave Id behind, since it should be first, isn't changing and is most complex.
            migrationBuilder.DropColumn(name: "RefreshBeginUtc", table: OriginalTable);
            migrationBuilder.DropColumn(name: "RefreshEndUtc", table: OriginalTable);
            migrationBuilder.DropColumn(name: "Database", table: OriginalTable);
            migrationBuilder.DropColumn(name: "HlaNomenclatureVersion", table: OriginalTable);
            migrationBuilder.DropColumn(name: "WasSuccessful", table: OriginalTable);
            migrationBuilder.DropColumn(name: "DataDeletionCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "DatabaseScalingSetupCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "DatabaseScalingTearDownCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "DonorHlaProcessingCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "DonorImportCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "MetadataDictionaryRefreshCompleted", table: OriginalTable);
            migrationBuilder.DropColumn(name: "QueuedDonorUpdatesCompleted", table: OriginalTable);
        }

        private static void ReAddAllColumns_IncludingNewColumns_InOrder(MigrationBuilder migrationBuilder)
        {
            //Id already in place
            migrationBuilder.AddColumn<string>(name: "Database", table: OriginalTable, nullable: false); // Note nullability has changed!
            migrationBuilder.AddColumn<bool>(name: "WasSuccessful", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SupportComments", table: OriginalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "RefreshBeginUtc", table: OriginalTable, nullable: false);
            migrationBuilder.AddColumn<DateTime>(name: "RefreshContinueUtc", table: OriginalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "RefreshEndUtc", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<string>(name: "HlaNomenclatureVersion", table: OriginalTable, nullable: true);  //Moved!
            migrationBuilder.AddColumn<DateTime>(name: "MetadataDictionaryRefreshCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "IndexDeletionCompleted", table: OriginalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "DataDeletionCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DatabaseScalingSetupCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DonorImportCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DonorHlaProcessingCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "IndexRecreationCompleted", table: OriginalTable, nullable: true); //New!
            migrationBuilder.AddColumn<DateTime>(name: "DatabaseScalingTearDownCompleted", table: OriginalTable, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "QueuedDonorUpdatesCompleted", table: OriginalTable, nullable: true);
        }

        private static void CopyClonedDataBackIntoTable(MigrationBuilder migrationBuilder)
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
SET IDENTITY_INSERT {OriginalTable} ON

INSERT INTO {OriginalTable}(
  {currentColumns}
)
SELECT
  {currentColumns}
FROM {CloneTable}

SET IDENTITY_INSERT {OriginalTable} OFF");
        }

        private static void ResetIdColumn(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DECLARE @max int = (SELECT COALESCE(MAX(Id), 1) FROM {OriginalTable});
DBCC CHECKIDENT({OriginalTable}, RESEED, @max)"
            );
        }

        private static void DeleteCloneTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TABLE {CloneTable}");
        }
        #endregion

        /// <remarks>
        /// Whilst the up-migration is very complex the down-migration doesn't need to be, because we don't care about column order on the way down.
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
