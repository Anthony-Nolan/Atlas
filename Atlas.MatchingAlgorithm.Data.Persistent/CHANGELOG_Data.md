# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 2.0.0
* Changes to contents of `MatchingAlgorithmPersistent.GradeWeightings` table:
  * Added new match grade, `ExpressingVsNull`.
  * Removed unused value, `PermissiveMismatch`.

## <= 1.5.0

Prior to v1.5 of Atlas, no Changelog was actively updated. A snapshot of the schema at this time has been documented here, and all future changes should be documented against the appropriate version.

### DataRefreshRecords

* `Id` = unique PK of the refresh attempt
* `RefreshRequestedUtc`- when the refresh was requested
* `RefreshLastContinuedUtc` - last time processing began for this request. May not match the requested time if a transient error occurred and the attempt resumed.
* `RefreshEndUtc` - when the refresh completed
* `RefreshAttemptedCount` - how many retries has this record needed
* `Database` - which of the matching databases (A or B) is this attempt against? 
* `HlaNomenclatureVersion` - hla nomenclature version used for processing. e.g. 3.36.0 would be stored as 3360
* `WasSuccessful` - true for success, false for a failure. null for in progress job
* Timing info - individual stages of the long-running refresh job are stored independently, to allow collection on relative timings of the stages 
  * `DataDeletionCompleted`
  * `IndexDeletionCompleted`
  * `DatabaseScalingSetupCompleted`
  * `MetadataDictionaryRefreshCompleted`
  * `DonorImportCompleted`
  * `LastSafelyProcessedDonor`
  * `DonorHlaProcessingCompleted`
  * `IndexRecreationCompleted`
  * `DatabaseScalingTearDownCompleted`
  * `QueuedDonorUpdatesCompleted`
* `SupportComments` - not used by the application. When manually running refresh jobs, this can be used to leave a comment for the supporter of the application, to explain the reasoning for the unscheduled job. 
* `ShouldMarkAllDonorsAsUpdated` - used by repeat search to determine whether all donors should be considered "updated" since searches run before the refresh, even if their HLA wasn't changed.


### Grade Weighting / Confidence Weighting

Used to convert from distinct match grade / confidence enum values from scoring results into a numeric "score" which can be easily consolidated across all loci, and compared to other donors for ranking. 
Configurable as different consumers of Atlas may want to assign different weights to the various values, as there is no "correct" answer for this ordering.

May want to use orders of magnitude for different "types" of value here - e.g. all serological-only match grades may want to be distinguishable from each other, but all significantly worse than non-serological grades. 

Both follow the same schema:

* `Id`
* `Name` must correspond to an enum value of the appropriate enum 
* `Weight` - numeric score to give this grade/confidence value