# Changelog
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

### 4.2.0

#### Match Prediction
* E: After a haplotype frequency set import, every app uses the new set within 1 minute of the success notification, not 5 minutes. The apps share the list of active sets through the distributed cache (Redis), when one is configured. See [the active set cache](/README_MatchPredictionAlgorithm.md#the-active-set-cache).
  * An import takes 30 seconds longer: 30 seconds after the success notification, it writes the list to the distributed cache again. If the cache does not store the list after 4 more writes, the import sends a medium-priority alert (see [README_Support](/README_Support.md#alert-the-new-active-sets-are-not-confirmed-in-the-distributed-cache)).
  * The default of the Terraform variable `MATCH_PREDICTION_ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES` changes from 5 to 1, and the match prediction worker container now uses it too. If your installation sets this variable, set it to 1: it is also how long an imported set can take to reach every app.
* E: New endpoint on the Match Prediction functions app (`<ENV>-ATLAS-MATCH-PREDICTION-FUNCTION`): `POST /api/RefreshActiveHaplotypeFrequencySetCache`. It writes the active sets from SQL to the distributed cache. Call it after a change of the active sets with SQL (see [Rolling back an upload](/README_Support.md#rolling-back-an-upload)), and after a restore or a copy of the match prediction database (see [README_Support](/README_Support.md#after-a-restore-or-a-copy-of-the-match-prediction-database)).
  * If the distributed cache is configured when you deploy this version, and a set is imported during the deployment, call this endpoint after the deployment. The apps of the old version do not write the distributed cache.
* E: The apps share the haplotype frequency sets that they load, through the distributed cache (Redis), when one is configured. An app that does not hold a set reads it from the distributed cache, and reads SQL only when the distributed cache does not have the set. So new app instances, and apps that load a set again, read SQL less often. See [the frequency set cache](/README_MatchPredictionAlgorithm.md#the-frequency-set-cache).
  * The distributed cache keeps each active set for 30 days. The 208 active sets of a copy of WMDA LIVE take about 106 MB in it.
  * After a restore or a copy of the match prediction database, the sets need no step: the key of a set in the distributed cache has its import time, not only its id, and each app checks the import time of each set that it uses once a minute (`MATCH_PREDICTION_ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES`), and loads a replaced set again.
* E: The frequency set cache of each app process now has a limit on memory, not on the number of sets: the sets differ in size by about a hundred times. The memory of a set is estimated from its number of haplotypes. See [the settings](/README_MatchPredictionAlgorithm.md#haplotypefrequencysetcachemaxcachedfrequencysetmegabytes--haplotypefrequencysetcachesetcacheexpiryminutes).
  * The Terraform variable `MATCH_PREDICTION_MAX_CACHED_FREQUENCY_SETS` (default `10`) is replaced by `MATCH_PREDICTION_MAX_CACHED_FREQUENCY_SET_MEGABYTES` (default `768`). 768 MiB holds 10 sets of the largest size, as the old default did, and about 32 sets of the average size. In the worst case, the sets of one process can use about 760 MiB, about 30 % more than with the old default.
  * The Terraform variable of the match prediction worker, `MATCH_PREDICTION_WORKER_MAX_CACHED_FREQUENCY_SETS` (default `30`, new in 4.1.0), is replaced by `MATCH_PREDICTION_WORKER_MAX_CACHED_FREQUENCY_SET_MEGABYTES` (default `832`). In a DEV search load, the 30 sets that a worker replica used most took at most 805 MiB, so 832 MiB holds them, as the old default of 30 sets did.
  * If your installation sets one of the old variables, set the new variable instead.
  * The functions apps that load frequency sets now check the `HaplotypeFrequencySetCache` settings when they start, as the workers do: an app without `HaplotypeFrequencySetCache:MaxCachedFrequencySetMegabytes` (for example, with a `local.settings.json` from an older version) does not start, and the error names the setting.
* E: The expiry of a frequency set in each app process is now sliding: a set that is not used for `MATCH_PREDICTION_SET_CACHE_EXPIRY_MINUTES` (default `60`) is removed, and a set in use stays. Before, a set was loaded again at the end of this time, also when it was in use: for example, each hour during stage 65 of a data refresh. The expiry of the match prediction worker, `MATCH_PREDICTION_WORKER_SET_CACHE_EXPIRY_MINUTES` (default `1440`), is sliding too.
* E: Haplotype frequency sets with the same frequencies share one cache entry in each app, one place in its memory limit, and one value in the distributed cache. An import makes one set for each registry and ethnicity pair of its file, and each of these sets has the same frequencies: the import now writes their content hash (`HaplotypeFrequencySets.ContentHash`) to all of them. On a copy of WMDA LIVE, the 208 active sets have 42 contents: an app loads each content once, all of them fit in the default memory limit, and the distributed cache holds about 14 MB of them, not 106 MB. Results do not change. See [the frequency set cache](/README_MatchPredictionAlgorithm.md#the-frequency-set-cache).
  * The apps read the frequencies of a set in the order of their ids (`ORDER BY Id`). This is the order in which they read them before; now SQL must keep it.
  * After you deploy this version, call the new endpoint on the Match Prediction functions app, `POST /api/BackfillHaplotypeFrequencySetContentHashes`, once: it writes the content hash of the sets of older imports, in the background, and a call shows the progress (see [README_Support](/README_Support.md#content-hashes-of-the-frequency-sets)). Until then, these sets work as before, each with an entry of its own.
  * Do not change the frequencies of a set with SQL: import a new file instead (see [README_Support](/README_Support.md#content-hashes-of-the-frequency-sets)).
* E: The apps read the frequencies of a haplotype frequency set from SQL with MARS (Multiple Active Result Sets) off, also when their connection string turns it on. With MARS, SQL Server waits for the client after a few packets, so the read of a large set waited for many network round trips. On a copy of WMDA LIVE, the read of the largest set (274,606 haplotypes) took 24.7 s with MARS and 0.5 s without it from a developer machine, and 7.2 s with MARS from a DEV app in Azure, in another region than the database. The connection strings do not change. See [the frequency set cache](/README_MatchPredictionAlgorithm.md#the-frequency-set-cache).

#### Distributed Cache
* E: Terraform now creates the distributed cache: an Azure Managed Redis per environment, `an-<ENV>-atlas-redis`, and configures the six apps that use it. They sign in with Microsoft Entra ID as `<ENV>-atlas-id-functions`; access keys are disabled. See [Distributed cache](/README_Deployment.md#distributed-cache-azure-managed-redis).
  * This is a new paid resource: by default `Balanced_B1` without high availability. Two new Terraform variables size it: `DISTRIBUTED_CACHE_SKU_NAME` (default `Balanced_B1`) and `DISTRIBUTED_CACHE_HIGH_AVAILABILITY_ENABLED` (default `false`).
  * In a production environment, set `DISTRIBUTED_CACHE_HIGH_AVAILABILITY_ENABLED` to `true` before you deploy this version. Changing it later recreates the cache.

#### Search Tracking
* E: The public API also sends each search request and repeat search request to Application Insights, as a custom event named `SearchRequested` (`customEvents | where name == "SearchRequested"`). This is the first step to build search tracking metrics in Application Insights. Search tracking keeps its SQL database, and the Service Bus message and the SQL row do not change.
  * The properties of the event are the fields of the search tracking event, with the same names and the same values as the Service Bus message: for example `SearchIdentifier`, `OriginalSearchIdentifier` and `DonorType`. A field without a value has no property. `AtlasLogger` also adds a `LogLevel` property, with the value `Info`.
  * The event has no patient information: it has no `RequestJson` property. The search request, with the patient's HLA typing, ethnicity code and registry code, stays only in the Service Bus message and the SQL row.
  * Search tracking events are not sampled, also when the app samples its other telemetry.
  * The event is sent at the `Info` level: an API app whose `ApplicationInsights:LogLevel` is above `Info` does not send it.

### 4.1.0

#### Debug
* BREAKING: The `Atlas.Debug.Client` and `Atlas.Debug.Client.Models` projects have been removed, and are no longer built or published. `4.0.0` is the last version that has them.
* BREAKING: The debug endpoints that only wrapped a resource (SQL, Service Bus, Blob Storage or Application Insights) have been removed from the Donor Import, Matching Algorithm, Repeat Search, Search Tracking and top-level functions apps.
  * [Atlas E2E tests](https://github.com/Anthony-Nolan/Atlas.Auto.Tests), the only consumer, now work with these resources directly.
  * The `debug` Service Bus subscriptions are kept, as the E2E tests read them directly.
* The debug endpoints that need Atlas code are kept: `ConvertHla`, `ScoringMetadata`, `Dpb1TceGroups` and `SerologyToAlleleMapping` on the Matching Algorithm functions app, and `Impute` and `MatchPatientDonorGenotypes` on the Match Prediction functions app.
  * BREAKING: The `CalculateGenotypeLikelihood` debug endpoint has been removed. It used a likelihood calculation that search does not use. Use `Impute`, which returns the likelihood of each imputed genotype from the calculation that search uses.
* E: New debug endpoint on the Data Refresh functions app (`<ENV>-DATA-REFRESH-FUNCTION`): `POST debug/precompute/genotypeSets/compute`. It computes the precomputed genotype set of a typing, or of up to 20 donors, live, with the engine of the pre-computation and of search. It returns the typing key, the haplotype frequency set, the genotype count, the sum of likelihoods, the size of the stored data, and on request the genotypes. With `Save`, it also stores the sets in the active matching database. A new app setting, `Debug:AllowPrecomputeWrites` (Terraform variable `MATCHING_DEBUG_ALLOW_PRECOMPUTE_WRITES`, default `false`), allows this. Turn it on in test environments only. The endpoint is in the Data Refresh app, not in the matching algorithm app, so that its imputations do not compete with searches. See the [Match Prediction README](/README_MatchPredictionAlgorithm.md#checking-stored-genotype-sets-debug-endpoint).
* E: With `CompareWithStored`, the same endpoint compares each computed set with its stored row, as search reads it: for a donor, through its link row; for a typing, for example a patient's, by the key of the live set. Each set gets a parity result (`Match`, `Mismatch` with the differences, `StaleFrequencySet`, `TypingChanged`, `NoStoredRow`, `DecodeFailed` or `Error`), and the response counts each result. With `Database` set to `Dormant`, it reads the dormant matching database at the nomenclature version of the latest refresh that wrote to it, so the rows of a data refresh can be checked before the databases swap. See the [Match Prediction README](/README_MatchPredictionAlgorithm.md#parity-check-comparewithstored).
* See [ADR 010](/ArchitecturalDecisionRecord/Phase2/010-Publish_Debug_Client.md#update-2).

#### Data Refresh
* The subscription of the Data Refresh functions app on the `data-refresh-requests` topic is renamed from `matching-algorithm` to `data-refresh`, as Data Refresh now has its own functions app.
  * Terraform deletes the old subscription and creates the new one. Messages in the old subscription, and in its dead-letter queue, are lost. Deploy when no data refresh is in progress, and with an empty dead-letter queue.
  * For local development, add a `data-refresh` subscription to the `data-refresh-requests` topic of your own Service Bus namespace.
* E: The donor genotype precomputation no longer stores the genotype set of a donor whose typing is unambiguous at small g group for a combination of loci: one small g group at both positions of every locus. Search computes such a set live without the haplotype frequency set, about as quickly as it reads a stored row. This removes many of the rows that the precomputation writes (about 61 % of the donor rows in a full DEV Data Refresh), but only a small part of the stored data. The donor import follows the same rule. In search, such a donor is counted as `NotStoredUnambiguous`, not `NoRow`, and its set is not stored back. The debug parity check reports `NotStoredUnambiguous` for it. See the [Match Prediction README](/README_MatchPredictionAlgorithm.md#donors-that-are-not-precomputed).
* E: The donor genotype precomputation stage stores its values without key-range locks. Before, each worker locked the gaps of the unique index around its new keys, so many workers waited for each other and deadlocked, mostly at the start of the stage, when the table is almost empty. Now a worker that loses a race on a key gets a unique index violation and stores the chunk again; the unique index still keeps one row per key. The donor import, search and the debug endpoint keep the locked insert.
* E: New app setting `DonorGenotypePrecomputation:Enabled` (default `true`) on the Data Refresh and Donor Management functions apps. Set it to `false` in an installation that does not use match prediction. Then Data Refresh stage 65 does no work and is marked as complete, and donor imports do not precompute genotype sets. A change of the setting does not delete the stored sets: the next data refresh removes them as before. Keep `MatchPrediction:Precompute:Mode` at `ForceLive` there. See the [Matching Algorithm README](/README_MatchingAlgorithm.md#turning-the-precomputation-off).

#### Matching Algorithm
* BF: Donor updates no longer reach the matching databases 5–30 min late (whole message lock periods), mostly after a restart of the donor management functions app. The donor update receivers no longer prefetch messages, and each receiver, including the one the Data Refresh app uses in its last stage, is closed when its run ends.

#### Match Prediction
* E: Faster live imputation in match prediction, when donor genotype sets are not read from the pre-computed tables.
  * Donor inputs are batched by haplotype frequency set (registry and ethnicity), so each batch needs fewer frequency sets.
  * The match prediction worker keeps more frequency sets in its cache, for longer. New Terraform variables `MATCH_PREDICTION_WORKER_MAX_CACHED_FREQUENCY_SETS` (default `30`) and `MATCH_PREDICTION_WORKER_SET_CACHE_EXPIRY_MINUTES` (default `1440`) set them for the worker container only. The match prediction functions app is not changed.
  * The haplotype frequencies of a donor's genotypes are read from the cache in one call per donor, not one call per haplotype.
  * The HLA metadata lookups that imputation uses are loaded in one go at the start of a batch that may impute live, instead of one row at a time. Only the nomenclature versions of the frequency sets the batch uses, and the matching version, are loaded.
  * The Multiple Allele Code table is checked once per process, not once per donor.
* E: The messages on the match prediction Service Bus topics now carry their ids as application properties, so you can see the ids without reading the message body (for example, in Service Bus Explorer or in a dead-letter queue). The message bodies do not change. Properties without a value are not added.
  * `match-prediction-requests` and `match-prediction-results`: `MatchPredictionRequestId` and `SearchRequestId`.
  * `parallel-match-prediction-requests`: `SearchRequestId`.
  * `parallel-match-prediction-results`: `SearchIdentifier`.

### 4.0.1
* BF: A data refresh to HLA nomenclature version 3660 no longer fails while it recreates the HLA Metadata Dictionary. Batch inserts into Azure Table Storage now also have a size limit, not only a limit of 50 rows. Before, a batch of large rows (such as the serology to alleles lookup) could exceed the 4 MiB limit of a table batch, and the storage service rejected it (`413 RequestBodyTooLarge`). This change also applies to the import of the Multiple Allele Code Dictionary, which uses the same batch insert.
* BF: Faster matching searches. The two indexes on the `Donors` table of the matching algorithm databases that the donor join uses now have covering columns: `IX_Donors_DonorType_RegistryCode` includes `DonorId`, and `IX_DonorId` includes `DonorType` and `RegistryCode`. Before, SQL Server had to read the table for every candidate donor, and searches could time out. See [the data changelog](/Atlas.MatchingAlgorithm.Data/CHANGELOG_Data.md#401) for the migration.

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
