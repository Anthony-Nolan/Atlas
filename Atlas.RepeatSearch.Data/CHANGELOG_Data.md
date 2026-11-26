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
* Added the column `LastUsedAt` (`datetimeoffset`, not null, default `SYSDATETIMEOFFSET()`) to `CanonicalResultSets`, with the index
  `IX_CanonicalResultSets_LastUsedAt`: when the set was last used, which is when its results were stored or when a repeat search of it last started.
  The retention job of the repeat search functions app deletes the sets that have not been used for `RepeatSearch:ResultsRetentionDays`, oldest first.
  * The migration fills `LastUsedAt` for the existing sets: the later of the date of the original search (`SearchTracking.SearchRequests.RequestTimeUtc`)
    and the date of the last repeat search (`RepeatSearchHistoryRecords.DateCreated`).
    A set with no search tracking row that was stored after search tracking started (for example after a lost tracking event) gets the latest
    request time of the tracked sets with a lower `Id` as the date of its original search: set ids grow with time, so this estimates when the set was stored.
    On a copy of WMDA LIVE, this applies to 73 of 280,445 sets, 47 of them stored in 2026.
    If no date is known, for a set stored before search tracking started, the set gets the date of the first search request in `SearchTracking.SearchRequests`,
    so that it counts as old. If there is no search tracking date at all, the set keeps the time of the migration.
  * The default is a runtime constant, so adding the column does not write the existing rows. The update that fills the column then writes each row of
    `CanonicalResultSets` once (about 253,000 rows on WMDA LIVE), and reads `SearchTracking.SearchRequests`. The index is created after the update.
* Added the status `2` = `Deleting` to `CanonicalResultSets.Status`: the retention job is deleting the rows of the set, in batches, and then the set row.
  Repeat searches do not use a set that is `Deleting`. The job sets this status in the same statement that checks `LastUsedAt`, and a repeat search sets
  `LastUsedAt` only on a `Complete` set, so a repeat search never reads a set that is partly deleted.
  * The job deletes the rows of `SearchResults` in batches of `RepeatSearch:ResultsDeleteBatchSize` (default 4,000), each in its own transaction.
    `RepeatSearchHistoryRecords` are kept. The database uses the freed space again, so no shrink is needed.

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