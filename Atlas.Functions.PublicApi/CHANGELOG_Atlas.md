﻿# Changelog
Changelog for Atlas as a product: it will cover functional and algorithmic changes that affect Atlas as a whole.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Product version is represented by the version tag of the `Functions.PublicApi` project.
The project version will be appropriately incremented with each change to the product and the nature of the changes logged below.

## Contents
- [Feature Flags](#feature-flags)
- [Version Logs](#version-logs)

## Feature Flags
Feature flags permit greater control over the release of new features (see this [ADR](/ArchitecturalDecisionRecord/Phase2/009-Feature_Flags.md) for more background). FFs are managed within Azure using an App Configuration resource (`[ENV]-ATLAS-APP-CONFIGURATION`). This section of the changelog documents feature flags, their state within an Atlas version (new (`N`), removed (`R`), or inherited from previous version (`P`)), and the ticket that describes their eventual removal ("retirement plan").

### 2.1.0
|State|Name|Component|Description|Retirement Plan|
|-----|----|---------|-----------|---------------|
|**R**|useDonorInfoStoredInMatchingAlgorithmDb|Matching Algorithm| IMPORTANT: Donor metadata will now _only_ be read from the matching db, instead of from the donor store, during the matching phase of search. |#1067|

### 1.6.0

|State|Name|Component|Description|Retirement Plan|
|-----|----|---------|-----------|---------------|
|N|useDonorInfoStoredInMatchingAlgorithmDb|Matching Algorithm|Enables reading of donor metadata from the matching db (instead of from the donor store) during the matching phase of search.|#1067|

## Version Logs

**Key** (in use from v1.6.0 onwards)
* E - Enhancement, i.e., new feature or extension of existing feature
* BF - Bug Fix
* BREAKING - Breaking change at the API level
* FBC - Functionally Breaking Change, i.e., significant change to the behaviour of an existing feature but the API remains unchanged

### 3.0.0
* E: Updated .NET version from 6.0 to 8.0

### 2.1.0 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/8?closed=1))
The main goal of this version was to add the debug endpoints needed to enable automated end-to-end testing (E2E) of an existing Atlas installation.
A new public, open-source project has been released to host Atlas E2E tests: https://github.com/Anthony-Nolan/Atlas.Auto.Tests

#### IMPORTANT
* FBC: The feature flag, `useDonorInfoStoredInMatchingAlgorithmDb` has been removed in this version ([see section above](#210)).
  - Donor metadata will now only be read from the matching algorithm db.
  - If upgrading Atlas from a version before `1.6.0`, when the flag was introduced, then you should:
    1) First upgrade Atlas to `v2.0.3` (i.e., the version previous to this one).
    2) Run a data refresh job to populate the new metadata columns in the `Donors` table.
    3) Finally, deploy Atlas `v2.1.0`.

#### Debug
* E: A host of new endpoints has been added for the debug of the most critical and oft-used production workflows: Donor Import, Search, Repeat Search and Scoring.
* E: New projects, `Atlas.Debug.Client` and `Atlas.Debug.Client.Models`, collate these debug endpoints into client libraries that can be used by the E2E tests.
  * These projects have been added to the build pipeline to generate NuGet packages and have their own changelogs.

#### WMDA Validation Set 4
* E: `ManualTesting` projects have been refactored and enhanced to allow running of the new WMDA validation dataset for exercise 4. See [Manual Testing README](/README_ManualTesting.md#match-prediction-validation-using-wmda-datasets) for more details.

#### Search Performance
* E: Reduced RAM usage by handling blob data using streams.

#### Bug Fixes
A variety of non-critical bug fixes:
* BF: Donor import message will no longer dead-letter due to `DuplicateDonorFileImportException`.
* BF: `StoreOriginalSearchResults` ignores multiple plays of the same `matching-results-ready` message and will no longer throw `DuplicateKeyException` in this edge case.
* BF: Failed `updated-searchable-donors` messages will now be applied upon replay (assuming the underlying issue has been addressed between attempts).
* BF: Exceptions thrown on startup of `MatchingAlgorithm.Functions.DonorManagement` app have been resolved by making `MessageReceiverFactory.GetMessageReceiver` threadsafe.

### 2.0.3 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/11?closed=1))
- E: Added new terraform var, ELASTIC_SERVICE_PLAN_FOR_PUBLIC_API, to control whether a separate elastic service plan will be created for the Public API app, or whether it will run on the same elastic plan as all other functions apps.
  - See [Configuration README](/README_Configuration.md#shared) for important release notes.
  - See [this ADR](/ArchitecturalDecisionRecord/Phase2/010-Elastic_Plan_for_Public_API_Optional.md) for general info.
  - Note: the existing terraform var, `SERVICE-PLAN-MAX-SCALE-OUT`, has been renamed to `ELASTIC-SERVICE-PLAN-MAX-SCALE-OUT`. It has the same default value of `50`.

### 2.0.2 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/10?closed=1))
#### Match Prediction
- E: Optimisation: do not load haplotype frequencies into memory when the subject is high-res typed and does not require imputation.

#### Support
- E: Added `InnerException` name to the App Insights custom event that logs HLA expansion failures.
- E: Added new endpoint to allow `updated-searchable-donors` message to be re-published for a given donor, in the event the original message failed to be applied. [See Support README for more info](/README_Support.md#re-publish-failed-updated-searchable-donors-messages).
- BF: For scheduled, automated data refresh attempts, do not throw an exception if a new HLA nomenclature version has not been detected.

### 2.0.1 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/9?closed=1))
- E: Atlas can now be configured to reject Full mode import files. See [Donor Import README](../README_DonorImport.md#full-mode) for more details.
- E: For donor import `EmptyDonorFileException`: alert description now contains the name of the failed file instead of the exception stack trace.

### 2.0.0 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/7?closed=1))
Major version number bump due to breaking change in the client. See [client changelog](../Atlas.Client.Models/CHANGELOG_Client.md) for details.

* E/BF: Null allele vs. expressing HLA typing is no longer assigned the grade of `Mismatch`, and instead is assigned the new grade, `ExpressingVsNull`.
   * The most visible outcome of this change is that the match count calculated by scoring will now align with that of matching in this edge case.
   * This can be interpreted as either an enhancement or a bug fix, depending on user expectations.

### 1.6.3 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/6?closed=1))
- E: Terraform-mediated migration of classic Application Insights to workspace-based Application Insights.
  - This required the creation of a new Log Analytics workspace, named `<env>-ATLAS`
  - New release variables have been added to determine the workspace daily quota (default: unlimited) and SKU (default: PAYG).
  - Note: terraform may recreate the feature flag, `useDonorInfoStoredInMatchingAlgorithmDb`, due to [this bug](https://github.com/hashicorp/terraform-provider-azurerm/issues/23315).
    - If so, the flag will need to be manually re-enabled post release.

### 1.6.2 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/5?closed=1))
- BF: Fix for a failing search request by enhancing the scoring HMD lookup to support ambiguous molecular typings that only expands to null alleles.

### 1.6.1 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/4?closed=1))
- E: Indexes on several matching algorithm db tables have been add/amended to improve performance, based on Azure recommendations.

### 1.6.0 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/2?closed=1))
This version has a significant set of changes that were prompted by the integration of Atlas into WMDA Search and Match. Several of them are around handling of searches with very large resultsets, improvements to donor import reporting, and bug fixes identified during testing.

#### Blob Storage
* BF: Stopped excessive number of `CreateContainer` API calls being made during blob download.

#### Search
* E: Ensure all failed search requests are reported as completed by routing dead-lettered search request messages to the `search-results-ready` topic, with the appropriate failure information.
* E: Added performance logs for initial search.
* BF: all search-related logs are now being tagged with the search request ID to allow complete end-to-end tracking of a search request via Application Insights.
* E: Added matching request start time to result set.
* BF: Allows matching requests to fail - and not stall - in the event of memory issues, by disabling Auto-Heal on matching algorithm function app.
* E: Specify additional subscriptions for `search-results-ready` topic via release var.
* E: Large search result sets can be written to multiple files. 
  * Result files will now be split into two types:
    * Search summary - this will be a single file containing all search result metadata, e.g., number of matches, the original search request, etc.
    * Search results - these will be written to 1 or more files, each containing a maximum number of results (this is a configurable setting `AzureStorage:BatchSize`).
  * Search results will still be written to one file along with search summary if batch size (`SearchResultsBatchSize`) is set to a value less than or equal to '0'. 
  * Default value for 'SearchResultsBatchSize' is '0'.
* BF: searches (and repeat searches) were failing when Atlas tried to lookup info on a matching donor that had been deleted from the donor store.
  - This problem was exacerbated by hundreds of thousands of donors updates being submitted to Atlas, which meant the matching algorithm was behind donor store for several hours.
  - Dependency on the donor store during search runtime has been removed. Now all donor metadata needed for search is stored within the matching algorithm db and added to the matching result model to be made available during match prediction and repeat search.

#### Repeat Search
* E: Ensure all failed repeat search requests are reported as completed by routing dead-lettered search request messages to the `repeat-search-results-ready` topic, with the appropriate failure information.
* E: Specify additional subscriptions for `repeat-search-results-ready` topic via release var.
* E: Ability to save search results in multiple files ([see Search section](#writing-of-search-results) for further info).
* BF: `StoreOriginalSearchResults` function can now successfully process failed searches and successful searches with no matching donors.

#### Scoring
* BF: DPB1 match category is now being calculated when the locus typing contained a single null allele, by treating the locus as homozygous for the expressing allele.
* E: Scoring feature now also calculates whether a position is antigen matched or not.
* **FBC: Positional locus scores are now aligned to donor typing, instead of to the patient typing.**
  * E.g., Given a locus with a single mismatch where patient typing 1 is mismatched to donor typing 2, `ScoreDetailsAtPositionTwo` will be assigned the mismatch score.

#### Donor Import
* E: Symmetric check of donors absence/presence in Atlas storage via new function `CheckDonorIdsFromFile`.
* E: Check of donor/CBU fields within Atlas donor store via new function `CheckDonorInfoFromFile`.
* E: Tagged donor import logs with the donor import file name.
* E: Improved reporting of donor import results:
  * Failed donor updates logged to a new table within shared database, `Donor.DonorImportFailures`.
    * **FBC**: Invalid donor creates/updates are now logged instead of throwing error.
  * New topic `donor-import-results` reports succcessful and failed import results.
  * New `FailedDonorCount` column added to `Donors.DonorImportHistory` to reflect number of donors were not updated.
* E: Automatically re-publish dead-lettered donor updates that failed to be consumed by the matching algorithm. This is distinct from "replay", as the update is regenerated from latest version of the data held in the donor store, which may have changed since the original dead-lettered update was published.

#### Match Prediction
* E: Prevent large result sets from blocking the completion of smaller search requests by improving the queuing of match prediction requests within activity functions.
* BF: Locus `PositionalMatchCategories` are now re-orientated in line with scoring results.
* E: Add file name to AI event that logs a failed HF set import.
* BF: Added measures to prevent the same HF set import request being processed by multiple workers which leads to database conflict errors.
* E: Haplotype frequencies from a failed file upload are deleted from the db as part of import clean up.
* E: Match probability can now be calculated for subjects typed with HLA that is invalid in haplotype frequency set HLA version, but is valid in matching algorithm HLA version.
* BF: Corrected the handling of subjects typed with a null allele that map to a small g group.

#### Data Refresh
* E: Job will automatically retry when the matching algorithm database is asleep/unavailable.

#### Manual Testing
- E: Exercises 1 and 2 of the WMDA consensus dataset can be run via new locally-running functions added to `Atlas.ManualTesting.Functions.WmdaConsensusDatasetFunctions`.
- E: Transform haplotype frequency dataset file that failed import due to the presence of an invalid typing via new locally-running function added to `Atlas.ManualTesting.Functions.HaplotypeFrequencySetFunctions`.
- E: Retrieve search performance and failure information via new locally-running function added to `Atlas.ManualTesting.Functions.SearchOutcomesFunctions`.

#### Debugging
- E: Http-triggered functions added for debugging of scoring and match prediction:
  - Matching algorithm functions app: GET `ScoringMetadata`, `Dpb1TceGroups`, and `SerologyToAlleleMapping`.
  - Match prediction functions app: POST `Impute` and `MatchPatientDonorGenotypes`.
- BREAKING: URL of all debug endpoints (including those added before this version) now have `/debug/` as a route prefix.


### 1.5.0 ([issue list](https://github.com/Anthony-Nolan/Atlas/milestone/1?closed=1))

#### API Documentation
* Added new http function that generates a JSON schema for the requested ResultSet client model.

#### Deployment & Integration
* Atlas client models and donor import schema now published as NuGet packages to simplify task of integration.
  * Build pipeline extended with tasks for generating NuGet packages.
  * Donor import file schema and "Common" models moved to new, standalone projects so they can be published as packages.

#### Donor Import
* Bug fix: New `DonorImport` function added to publish the donor update messages that keep the matching algorithm donor store in sync with the donor import donor store. This is to prevent messages from being lost if the app restarts during donor import. A second timer function cleans up expired updates to keep the update repository from getting too large.
* Donor import validation errors now logged as custom events to Application Insights.

#### Manual Testing
* New projects have been added to permit the validation of the match prediction algorithm using an externally generated dataset.

#### Matching Algorithm
* Fixed bug where, in certain cases, potential and exact match counts per donor were not being calculated correctly.
* Matching algorithm results notification extended with failure information, including the number of times a failed search has been attempted thus far, and how many attempts remain.

#### Match Prediction
* New endpoint added that allows match prediction to be performed without running a full search. It accepts batches of match prediction requests: one patient vs. a set of donors. Results are written out to blob storage, and a notification sent to a new topic: `match-prediction-results`.
* Fix for bug where predictive match categories were not being consistently applied to different loci, despite them having the same match probability percentage values.

#### Search
* Search results notification has been extended with failure information, including:
  * the stage of failure,
  * the number of times matching has been attempted and how many attempts remain,
  * and whether search as a whole will be retried.

#### Support
* Decreased Time To Live on `audit` service bus subscriptions to avoid topic maximum size limit being reached due to old messages not being cleared. The value has been set to 14 days, which should be enough time for debug/support purposes.

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
