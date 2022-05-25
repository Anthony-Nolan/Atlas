# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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