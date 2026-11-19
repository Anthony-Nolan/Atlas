# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## 4.2.0
* Added the column `Status` (`int`, not null, default `1`) to `CanonicalResultSets`: `0` = `InProgress` while the results of the original search
  are stored in chunks, `1` = `Complete` when all of them are stored. Existing sets get the default, `Complete`. Repeat searches only use complete sets.
* Added the nullable column `StoreAttemptId` (`uniqueidentifier`) to `CanonicalResultSets`: the delivery of the results message that is storing the set.
  A later delivery that finds the set in progress takes it over with a new id; only the delivery with the current id can start a chunk and complete the set.
  A chunk that a superseded delivery started just before the take-over can still commit: the unique index on (`CanonicalResultSetId`, `ExternalDonorCode`)
  of `SearchResults` then makes one of the two inserts of the same code fail. Do not remove this index.
* Replaced the unique index of `SearchResults` on (`ExternalDonorCode`, `CanonicalResultSetId`) by a unique index on (`CanonicalResultSetId`, `ExternalDonorCode`),
  `IX_SearchResults_CanonicalResultSetId_ExternalDonorCode`, and dropped `IX_SearchResults_CanonicalResultSetId`, which the new index makes redundant.
  Set ids grow, so the rows of a new set now go to the end of the index, and an insert no longer reads a random page of the index for each row.
  * The migration builds the new index online (`ONLINE = ON`) before it drops the old ones, so the apps can keep working during the build.
    The build reads the whole table, and needs free space in the database for the new index, about the size of the old unique index,
    until the old indexes are dropped. After the migration, the indexes of the table are smaller by about the size of `IX_SearchResults_CanonicalResultSetId`.
  * The release runs each migration in its own transaction, so the build runs in the transaction of this migration. On a large table, build the new index by hand before the release,
    out of hours, with the SQL below. The migration then skips the build, and only drops the old indexes. `RESUMABLE = ON` lets you pause the build
    (`ALTER INDEX ... PAUSE`) and resume it later (`ALTER INDEX ... RESUME`).
    On a copy of the WMDA LIVE repeat search database (741 million rows in `SearchResults`, Azure SQL S7), the build took about 5.5 hours.
    It was limited by the log write rate of the database, and used up to about 44 GB more space. Dropping the old indexes then took less than a second.
    After the change, storing the results of an original search with 275,527 donors took 24 seconds instead of 447 seconds.
    ```sql
    CREATE UNIQUE INDEX [IX_SearchResults_CanonicalResultSetId_ExternalDonorCode]
    ON [RepeatSearch].[SearchResults] ([CanonicalResultSetId], [ExternalDonorCode])
    WITH (ONLINE = ON, RESUMABLE = ON);
    ```
  * Just before the release, check that the manual build is complete: this query must return no row.
    A pause, or a failure such as a failover, leaves a resumable build paused, not rolled back. While it is paused, DDL on the table is not allowed,
    so the migration would fail in the release.
    ```sql
    SELECT name, state_desc, percent_complete FROM sys.index_resumable_operations;
    ```
    To continue a paused build, run the `CREATE INDEX` statement above again, or:
    ```sql
    ALTER INDEX [IX_SearchResults_CanonicalResultSetId_ExternalDonorCode] ON [RepeatSearch].[SearchResults] RESUME;
    ```

## 3.0.0
* Updated .NET version from 6.0 to 8.0

## <= 1.5.0

Prior to v1.5 of Atlas, no Changelog was actively updated. A snapshot of the schema at this time has been documented here, and all future changes should be documented against the appropriate version.

### CanonicalResultSets

Repeat search keeps track of the result set for a search request, to allow us to identify donors removed from the result set. 

* `Id`
* `OriginalSearchRequestId` - the search request Id provided to the consumer of Atlas

### Search Results

Results stored in a canonical result set. This is a very minimal set of result info, only used for determining result diffs. Full results are still to be procured from results files in azure storage

* `Id`
* `CanonicalResultSetId` - FK to CanonicalResultSets
* `ExternalDonorCode` - external donor code (provided by consumer, not internal Atlas ID)

### RepeatSearchHistoryRecords

* `Id`
* `OriginalSearchRequestId` - the search request Id provided to the consumer of Atlas for the original search
* `RepeatSearchRequestId` - search request Id provided to the consumer of Atlas for this instance of a repeat search
* `SearchCutoffDate` - cutoff provided for the repeat search
* `DateCreated`
* `UpdatedResultCount` - how many search results in the set have been updated 
* `AddedResultCount` - how many search results in the set have been added
* `RemovedResultCount` - how many search results in the set have been removed