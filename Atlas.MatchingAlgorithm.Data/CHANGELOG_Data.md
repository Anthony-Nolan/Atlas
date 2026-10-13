# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 4.1.0

Four new tables for the donor genotype precomputation stage of the data refresh. Run the migration on both transient
databases. Nothing writes to them until that stage is released, and each data refresh wipes them.

### DonorGenotypePrecomputationRuns

One row per run of the stage. The stage resumes from it.

* `Id`
* `DataRefreshRecordId` - unique; the `DataRefreshHistory` record of the refresh
* `HlaNomenclatureVersion` - the version that every value of the run is imputed under
* `Status` - `Building`, `Running`, `Completed`, `CompletedWithFailures` or `Cancelled`
* `GroupsPerBatch`, `TotalGroupCount`, `TotalBatchCount`, `TotalDonorAssignmentCount`
* `TotalDonorCount` - the donors of the build: the base of the failed-donor fraction
* `ManualRetryCount` - how many times a manual retry sent the failed batches again
* `CreatedUtc`, `StatusDateUtc`, `CompletedUtc`

### DonorGenotypePrecomputationBatches

One row per worker message: a contiguous range of group ids.

* `Id`, `RunId`, `BatchNumber` - (`RunId`, `BatchNumber`) is unique
* `FirstGroupId`, `LastGroupId`, `GroupCount`, `DonorAssignmentCount`
* `Status` - `Pending`, `Requested`, `InProgress`, `ResultsReceived`, `Failed`, `Abandoned` or `PermanentlyFailed`
* `RetryCount`, `FailureMessage`, `FailureException`, `FailedGroupCount`
* `LeaseOwner`, `LeaseExpiresUtc` - the worker that holds the batch
* `DispatchedUtc`, `StatusDateUtc`, `CompletedUtc`

### DonorGenotypePrecomputationGroups

Staging, truncated when the run completes. One row per imputation: the donors that share a typing (restricted to the
loci of `AllowedLociKey`), a registry and an ethnicity.

* `Id` - given by the build, contiguous per batch; not an identity
* `RunId`, `AllowedLociKey`, `RepresentativeDonorId`, `DonorCount`
* `SubjectGenotypeSetValueId` - the stored value, once a worker has computed it
* `FailureMessage` - set when the value could not be computed

### DonorGenotypePrecomputationGroupDonors

Staging, truncated when the run completes. The donors of each group: four rows per donor.

* `GroupId`, `DonorId` - the clustered primary key

## 3.0.0
* Updated .NET version from 6.0 to 8.0

## 2.1.0
- Index added to `ExternalDonorCode` column in `Donors` table.

## 1.6.1
- Indexes on several tables have been add/amended to improve performance, based on Azure recommendations.
  - Existing index on DonorManagementLogs table will be dropped and recreated via EF migration.
  - New indexes on matching HLA tables will only be added during the next run of data refresh.

## 1.6.0

### Donors

New columns have been added

* `ExternalDonorCode` - unique identifier for donors provided by the consumer. String - can be numeric but does not need to be. 
* `EthnicityCode` - string representation of donor ethnicity
* `RegistryCode` - string representation of donor source registry

## <= 1.5.0

Prior to v1.5 of Atlas, no Changelog was actively updated. A snapshot of the schema at this time has been documented here, and all future changes should be documented against the appropriate version.

### Donors

Copy of donor store from the persistent donor table, allowing for FK relationships to pre-processed search data

* `Id` - PK, unique to matching component. Should not leave matching algorithm
* `DonorId` - **Internal Atlas** id of a donor. (**NOT** external donor id - this must be looked up in the master donor store before leaving Atlas)
* `DonorType` - Adult or Cord
* `IsAvailableForSearch` - allows "soft-deletion" of donors, which is more efficient than removal in bulk
* HLA Data
* `A_1`
* `A_2`
* `B_1`
* `B_2`
* `C_1`
* `C_2`
* `DPB1_1`
* `DPB1_2`
* `DQB1_1`
* `DQB1_2`
* `DRB1_1`
* `DRB1_2`

### DonorManagementLogs

Log of when donors were last updated, to enforce donor updates are applied in strict order.

- `Id`
- `DonorId` - *matching algorithm* assigned donor id from [Donors](#donors)
- `SequenceNumberOfLastUpdate` - id from service bus message triggering the latest update for the donor (not used in practice)
- `LastUpdateDateTime` - time this donor was last updated

### PGroupNames

Normalisation of P-Group strings

* `Id`- internal matching algorithm specific id for a p-group
* `Name` - name used by official nomenclature for this p-group

### HlaNames

Normalisation of HLA names seen in all donor typings

* `Id`- internal matching algorithm specific id for a hla name
* `Name` - name used by official nomenclature for this hla name

### MatchingHlaAtX

One table per supported locus. Used for relating a donor's HLA at each position to the denormalised hla name table

* `Id`
* `TypePosition` - position within locus - 1 or 2
* `DonorId` - FK to Donors
* `HlaNameId` - FK to HlaNames

### HlaNamePGroupRelationAtX

One table per supported locus. Used to relate all known hla names to one or more possible P-groups.

* `Id` 
* `HlaNameId` - FK to HlaNames 
* `PGroupId` - FK to PGroupNames
