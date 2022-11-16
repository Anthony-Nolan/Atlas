# Changelog
Changelog for Atlas as a product: it will cover functional and algorithmic changes that affect Atlas as a whole.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning
Product version is represented by the version tag of the Functions.PublicApi project.
The project version will be appropriately incremented with each change to the product, and the nature of the change logged below.

## Versions

### 1.5.0

#### Support
* Decreased Time To Live on `audit` service bus subscriptions to avoid topic maximum size limit being reached due to old messages not being cleared. The value has been set to 14 days, which should be enough time for debug/support purposes.

#### Donor Import
* Bug fix: New `DonorImport` function added to publish the donor update messages that keep the matching algorithm donor store in sync with the donor import donor store. This is to prevent messages from being lost if the app restarts during donor import. A second timer function cleans up expired updates to keep the update repository from getting too large.

#### Match Prediction
* New endpoint added that allows match prediction to be performed without running a full search. It accepts batches of match prediction requests: one patient vs. a set of donors. Results are written out to blob storage, and a notification sent to a new topic: `match-prediction-results`.
* Fix for bug where predictive match categories were not being consistently applied to different loci, despite them having the same match probability percentage values.

#### Manual Testing
* New projects have been added to permit the validation of the match prediction algorithm using an externally generated dataset.

### 1.4.2

#### HLA Metadata Dictionary
* Fix for bug where HMD lookup failed for a decoded MAC that included a deleted allele with an expression suffix.

### 1.4.1

#### Deployment
* Fixes made to build pipelines and terraform files.

### 1.4.0

#### Matching Algorithm

* New Batch scoring endpoint added, to allow standalone scoring feature to be run on multiple donors at once
* Performance improvements greatly improve speed of 1-3 mismatch searches, particularly in very large installations

### 1.3.0

#### Technical 

* Framework updated from .det core 3.1 to .net 6.0.
* Azure functions SDK updated from v3 to v4.

#### Donor Import

- New "changeType" supported for donor import files = `NU` = Upsert ("new or update") - allowing a consumer to provide a donor that should be added or updated, without caring whether that donor was already tracked by Atlas. 
* New config settings added to allow disabling of notifications when: 
  * File successfully imported
  * Donor deletions were attempted for donors that were not tracked in Atlas

#### Matching Algorithm

* Bug fixed where overall match confidence could be assigned "Permissive Mismatch" when non-DPB1 mismatches were known to be present

#### Match Prediction

* Major performance improvements have significantly reduced the time taken for match prediction with large haplotype frequency sets
* Bug fixed where some haplotypes were included twice in the probability calculations

#### MAC Dictionary

* Alerts are now sent when the MAC dictionary import fails

#### Search 

* Atlas can now filter donor results based on registry codes
* Dpb1 Mismatch Direction is now returned in Scoring results from searches.
* Search result now contains details about the search criteria used to initiate the search.
* Search result now contains details about the Haplotype Frequency sets used for match prediction, for both patient and donor results.

### 1.2.0

- Fixed scoring issue in which some DPB1 pairs were erroneously classified as a Non-Permissive Mismatch, when in reality they should be Permissive.
- `PermissiveMismatch` match grade has been removed and will no longer be assigned - see [Client Changelog](../Atlas.Client.Models/CHANGELOG_Client.md) for more details on this change. 

### 1.1.1

- All enum values will now be serialised to strings, to allow ease of parsing the serialised results files / http responses for external consumers, and for human-readability.

### 1.1.0

#### Changed
- Matching and Match Prediction algorithms are now able to run at different HLA nomenclature versions.
  - MPA will now use the HLA versions of the haplotype frequency sets referenced during match probability calculations.
  - Matching will continue to use the HLA version that was set at the time of the last successful data refresh.

### 1.0.1

#### Fixed
- Fix for bug that was preventing HLA metadata dictionary refresh to v3.44.0 of HLA nomenclature.

### 1.0.0

- First stable release of the Atlas product.