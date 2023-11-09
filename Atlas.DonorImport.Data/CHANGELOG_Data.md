# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versions

### 1.6.0
* New data table added, `DonorImportFailures`, to capture info on individual donor updates that failed to be applied to the donor store.
  * `Id` - unique PK
  * Columns holding data captured from the donor update:
    * `ExternalDonorCode`
    * `DonorType`
    * `EthnicityCode`
    * `RegistryCode`
  * `UpdateFile` - name of source import file
  * `UpdateProperty` - donor update property that failed validation, if relevant
  * `FailureReason` - reason why update was not applied
  * `FailureTime` - datetime stamp of failure
* Added new column to `DonorImportHistory` table.
  * `FailedDonorCount` - number of donors were not updated

### 1.5.0

* New data table added, `PublishableDonorUpdates`. Donor updates are written to the table during donor import; thereafter they are read and published by the new Donor Updates function.
  * `Id` - unique PK
  * `SearchableDonorUpdate` - Donor update message to be published.
  * `DonorId` - Internal, Atlas-assigned donor ID.
  * `CreatedOn` - DateTime UTC that record was added to the table.
  * `IsPublished` - Flag denoting whether an update has been published yet or not. 
  * `PublishedOn` - DateTime UTC when an update was published, if `IsPublished` is `true`.


## <= 1.5.0

Prior to v1.5 of Atlas, no Changelog was actively updated. A snapshot of the schema at this time has been documented here, and all future changes should be documented against the appropriate version.

### Donors

Master store of donor details imported to Atlas

* `AtlasId` = unique PK identifier for the donor, assigned by Atlas and used to refer to donors internally. Numeric
* `ExternalDonorCode` - unique identifier for donors provided by the consumer. String - can be numeric but does not need to be. 
* `DonorType` Adult vs Cord donor type enum, stored as backing int.
  * `Adult` = 0
  * `Cord` = 1
* `EthnicityCode` = String representation of donor ethnicity
* `RegistryCode` = String representation of donor source registry
* HLA, in string format at supported loci
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
* `Hash` - hash of all tracked donor details, to efficiently identify updated records
* `UpdateFile` - which file this donor was last updated by
* `LastUpdated` - when was this donor last updated

### DonorImportHistoryRecord

Contains records of all processed donor import files

* `Id` - unique PK of the file import record
* `ServiceBusMessageId` - Id of the service bus message that triggered the import. Not useful in practice.
* `Filename` - File name of the imported file
* `UploadTime` - The time a file was uploaded to blob storage
* `FileState` - enum, string representation. Allowed values:
  * `Started`
  * `Completed`
  * `FailedPermanent`
  * `FailedUnexpectedly`
  * `Stalled`
* `LastUpdated` - last update to this row 
* `ImportBegin` - when processing of this file began
* `ImportEnd` - when processing of this file ended
* `FailureCount` - how many times has an attempted import of this file failed processing. (Files can fail and be retried if transient errors e.g. connectivity issues prevent a full import)
* `ImportedDonorsCount` - how many donors have been imported from the file. Allows partial processing of a file on retry after a transient error.

### DonorLogs

A record of "last imported" data on a per donor basis. 

Used to enforce that donor updates are not applied out of sequence.

* `ExternalDonorCode` - external donor identifier
* `LastUpdateFileUploadTime` - last time this donor was updated
