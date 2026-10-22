# Configuration

This document covers tuning the configuration of Atlas to suit your needs. 

* PREVIOUS: [Deployment](./README_Deployment.md)
* NEXT: [Integration](./README_Integration.md)

_________

Where possible, Atlas should be configured via **terraform variables** set at release time - 
no Azure configuration should be manually performed, unless the config in question is not supported by Terraform.

If you come across configuration requirements of your installation that are *NOT* supported, please raise
an issue in this repository, so that such configuration can be added. 

All configurable elements of Atlas are available as terraform variables, exposed in the
[core terraform variables file](terraform/core/variables.tf). 

This file specifies the expected type of the variable, as well as a short description of how it will be used.


The rest of this document is an extension of this documentation, aiming to highlight logical groups of 
configuration options, and when you may want to use them.

**FORMATTING NOTE**: All Terraform settings use `_` as a delimiter. Functions app setting names use `:`, and `__`. In this document both have been replaced with `-`, to allow Github's markdown renderer to display these tables in a 
readable manner.

**SECRETS NOTE**: Settings that carry a shared secret - the storage account connection string and the Service Bus
connection strings - are not set to a literal value. Their value is an `@Microsoft.KeyVault(...)` reference, resolved at
runtime from the environment's Key Vault. To change one of these, change the secret in the vault rather than the app
setting; see [Key Vault bootstrap](./README_Deployment.md#key-vault-bootstrap).

## Feature functionality

| Terraform Setting          | Functions App Name | Functions App Setting Name | Description |
| -------------------------- | ------------------ | -------------------------- | ----------- | 
| IP-RESTRICTION-SETTINGS | all | N/A | Allows restriction of functions app access to specified IPs only. |
| LOG-ANALYTICS-DAILY-QUOTA-GB | N/A | N/A | The Log Analytics workspace daily quota for ingestion in GB. Default is -1 (unlimited). |
| LOG-ANALYTICS-SKU | N/A | N/A | Log Analytics Workspace SKU. Default is `Pay As You Go`. |
| MATCHING-DATA-REFRESH-AUTO-RUN | DATA-REFRESH-FUNCTION | DataRefresh-AutoRunDataRefresh | When set, the data refresh will automatically run once new HLA nomenclature is detected. This can be disabled to allow manual control on when new nomenclature is imported. |
| MATCHING-DEBUG-ALLOW-PRECOMPUTE-WRITES | DATA-REFRESH-FUNCTION | Debug-AllowPrecomputeWrites | When set, the genotype set compute debug endpoint can save genotype sets to the active matching database, where search reads them. Default is `false`. Set it in test environments only. See [the Match Prediction README](README_MatchPredictionAlgorithm.md#checking-stored-genotype-sets-debug-endpoint). |

## Donor Import
Settings for the donor import app (`ATLAS-DONOR-IMPORT-FUNCTIONS`).

| Terraform Setting          | Functions App Setting Name | Description                |
| -------------------------- | -------------------------- | -----------                | 
| DONOR-IMPORT-NOTIFICATIONS-ON-DELETION-OF-INVALID-DONOR | NotificationConfiguration-NotifyOnAttemptedDeletionOfUntrackedDonor | When enabled, notifications will be sent if an import file attempts to delete a donor that was not tracked in Atlas. |
| DONOR-IMPORT-NOTIFICATIONS-ON-SUCCESSFUL-IMPORT | NotificationConfiguration-NotifyOnSuccessfulDonorImport | When enabled, notifications will be sent for every imported donor file. May want to be disabled if donor imports are very frequent. |
| DONOR-IMPORT-ALLOW-FULL-MODE-IMPORT | DonorImport-AllowFullModeImport | Controls whether Full mode donor import files will be accepted (true) or rejected (false) |

### Publishing Donor Updates
Donor updates are published during donor import, and are consumed by the matching algorithm component, to keep its donor table in sync with that of donor import. The settings below control how often updates are published, and when published updates should be cleaned.

| Terraform Setting          | Functions App Setting Name | Description                |
| -------------------------- | -------------------------- | -----------                | 
| DONOR-IMPORT-DELETE-PUBLISHED-DONOR-UPDATES-CRONTAB | PublishDonorUpdates-DeletionCronSchedule | Crontab used to determine how often to delete expired, published donor updates. |
| DONOR-IMPORT-PUBLISH-DONOR-UPDATES-CRONTAB | PublishDonorUpdates-PublishCronSchedule | Crontab used to determine how often to check for and publish new donor updates. |
| DONOR-IMPORT-PUBLISHED-UPDATE-EXPIRY-IN-DAYS | PublishDonorUpdates-PublishedUpdateExpiryInDays | Number of days after publishing that a donor update will expire and be eligible for deletion. If not set, then no deletions will occur (warning, in this case, the updates table will continue to grow and this may impact update publishing performance after time). |

## Performance / Scale Tuning

This section is intended to describe the settings that may need to be tweaked to handle differently 
sized datasets (i.e. larger numbers of searchable donors), and to handle differing loads (e.g. 
an environment running constant difficult searches will want to be set up differently to one that 
infrequently runs easy searches) 

### Data Refresh

| Terraform Setting          | Functions App Name                 | Functions App Setting Name | Description                |
| -------------------------- | ------------------                 | -------------------------- | -----------                | 
| MATCHING-DATA-REFRESH-DB-SIZE-DORMANT | DATA-REFRESH-FUNCTION | DataRefresh-DormantDatabaseSize | The size of database used for the dormant (inactive) matching database. Recommended to be the lowest tier that allows `MATCHING-DATABASE-MAX-SIZE`, to save on hosting costs: the dormant database keeps its matching data and stored donor genotype sets until its next refresh. S0 allows 250 GB at most. |
| MATCHING-DATA-REFRESH-DB-SIZE-REFRESH | DATA-REFRESH-FUNCTION | DataRefresh-RefreshDatabaseSize | The size of database used for the matching database during the data refresh. Recommended to be fairly powerful to facilitate a fast data refresh. Premium tier recommended over Standard, for improved IO performance. Must allow `MATCHING-DATABASE-MAX-SIZE`. |
| MATCHING-PRECOMPUTE-WORKER-CONTAINER-MAX-REPLICAS | atlas-precompute-ca (Container App) | N/A | How many replicas of the donor genotype precompute worker can run at once during the precomputation stage of the data refresh. The minimum is always 0, so the worker costs nothing outside a refresh. More replicas finish the stage sooner, but each one adds load on the database being refreshed - keep `MATCHING-PRECOMPUTE-WORKER-MAX-CONCURRENT-CALLS` times this within that database's worker and session limits. |
| MATCHING-PRECOMPUTE-WORKER-MAX-CONCURRENT-CALLS | atlas-precompute-ca (Container App) | PrecomputeWorker__MaxConcurrentCalls | How many precomputation batches each worker replica processes at once. Recommended to stay at 1, as imputation is memory intensive; scale out with replicas instead. |
| MATCHING-PRECOMPUTE-WORKER-BATCH-LEASE-MINUTES, MATCHING-PRECOMPUTE-WORKER-MAX-AUTO-LOCK-RENEWAL-MINUTES | atlas-precompute-ca (Container App) | PrecomputeWorker__BatchLeaseMinutes, PrecomputeWorker__MaxAutoLockRenewalMinutes | A precomputation batch must finish within both, or it is given to another worker and computed twice. Keep the lease no greater than the lock renewal, and raise both together if batches take longer. |

### Shared 

The shared Atlas SQL server database will impact the performance of both algorithms (matching and match prediction), as well as donor and HF set import processes.
Most Atlas functions apps run on a shared service plan, which will also impact the performance of multiple apps. 

| Terraform Setting          | Functions App Name                 | Functions App Setting Name | Description                |
| -------------------------- | ------------------                 | -------------------------- | -----------                | 
| DATABASE-SHARED-EDITION | N/A | N/A | Determines the "edition" or pricing model of the shared database. "Standard" is used for the fixed price DTU based model, and "GeneralPurpose" is used for vCore (including serverless/auto-scaling) based pricing. Serverless tier is recommended for most installations. |
| DATABASE-SHARED-MAX-SIZE | N/A | N/A | Will only need changing if the amount of data in your data set (likely mostly donor data) is close to exceeding the default. **WARNING**: Azure is *very* picky about this setting - it must match an allowed value *to the byte* - or this variable will be silently ignored.  |
| DATABASE-SHARED-SKU-SIZE | N/A | N/A | Determines the size of the Azure SQL database tier, e.g. S2/P3/GP_S_Gen5_2 |
| ELASTIC-SERVICE-PLAN-FOR-PUBLIC-API | N/A | N/A | Determines whether a separate elastic plan should be created that only hosts the `<env>-ATLAS-API` app (`true`), or whether the API app should run on the same elastic plan as all other function apps (`false`). Default is `true`.<br><br>**Important Release Notes**:<br>On _first release_ of this var (added in [Atlas v2.0.3](/Atlas.Functions.PublicApi/CHANGELOG_Atlas.md#203)):<br>* The default value of `true` should be used. This will apply various minor, but necessary terraform state changes, but no significant infrastructure changes.<br><br>On subsequent releases:<br>* The var can be set to `false` whenever you wish the `<env>-ATLAS-API` app to run on the same plan as all other function apps.<br>* **WARNING**: changing the value of the `bool` var, in either direction, will cause the `<env>-ATLAS-API` functions app to be recreated and its **host keys to be regenerated**. <br>* The `WEBSITE-RUN-FROM-PACKAGE` var must be set to `0` within the same release to allow host key generation; it can be reset to `1` thereafter. |
| ELASTIC-SERVICE-PLAN-SKU-SIZE | N/A | N/A | Determines the size of the service plan running Atlas functions. |
| ELASTIC-SERVICE-PLAN-MAX-SCALE-OUT | N/A | N/A | Determines the number of instances that can be horizontally scaled to run Atlas. Matching will be strictly limited by other concurrency settings, which are likely to be below this service-plan level restriction - so this mostly affects match prediction, which will scale out as wide as the service plan allows. Running more instances will incur higher costs, but they should only be scaled out in periods of high load - in which case they will also help get through the load quicker, so the overall impact to pricing should be negligible when this is changed. |

### Matching Algorithm

| Terraform Setting          | Functions App Name                 | Functions App Setting Name | Description                |
| -------------------------- | ------------------                 | -------------------------- | -----------                | 
| MATCHING-BATCH-SIZE | ATLAS-MATCHING-ALGORITHM-FUNCTIONS | MatchingConfiguration-MatchingBatchSize | Determines the limit to donors loaded into memory during the matching process. The default value of 250,000 has been selected to give a good balance between performance and memory usage. Increasing this batch size should bring performance improvements, at the expense of a higher memory footprint - and if the memory footprint is allowed to be too high, there is a risk of out of memory exceptions stopping searches working.| 
| MATCHING-DATABASE-MAX-SIZE | N/A | N/A                            | Determines *storage* size of each transient matching database, in GB. The matching data needs ~1.5GB per million donors, though this will vary based on donor genotype ambiguity. The data refresh also stores donor genotype sets in the database: for about 44 million donors, an estimated 377 GB at the peak - see step 5 of [releasing the donor genotype precomputation](README_Deployment.md#releasing-the-donor-genotype-precomputation-data-refresh-stage-65). The value must be no more than the limit of the active, dormant and refresh tiers, as the data refresh changes the tier but not the max size; Terraform checks this at plan time. For vCore (e.g. serverless) tiers, Azure bills storage on this max size, not on the space used. |
| MATCHING-DATABASE-STORAGE-ALERT-THRESHOLD-PERCENT | N/A | N/A | The Atlas team is alerted when a transient matching database uses more than this percentage of `MATCHING-DATABASE-MAX-SIZE`. Default 85. No alert is created when `USE-EXTERNAL-SQL` is true. |
| MATCHING-MAX-CONCURRENT-PROCESSES-PER-INSTANCE | ATLAS-MATCHING-ALGORITHM-FUNCTIONS |AzureFunctionsJobHost--extensions--serviceBus--messageHandlerOptions--maxConcurrentCalls|Determines how many searches can run on a single instance of the matching algorithm. Higher values will improve throughput without increasing cost, but will decrease performance when multiple searches are running on the same instance, and increase [load on the matching database](#database-connection-limit). In environments with a very large number of donors, memory will be a significant concern, and this concurrency should be  kept fairly low (e.g. 1-2) |
| MATCHING-MAX-SCALE-OUT | ATLAS-MATCHING-ALGORITHM-FUNCTIONS | WEBSITE-MAX-DYNAMIC-APPLICATION-SCALE-OUT | How many instances of the matching algorithm can be run at once? These will automatically scale up with load, up to this limit. A higher value here means higher throughput with less impact on individual search times, though comes with increased running cost, and increased [load on the matching database](#database-connection-limit) |
| MATCHING-DATABASE-TRANSIENT-TIMEOUT | ATLAS-MATCHING-ALGORITHM-FUNCTIONS | SqlA/SqlB | Timeout for any individual query of the matching database. Default of 30 minutes should be enough in all but the largest Atlas installations, which may require slightly longer timeouts. **WARNING** If the matching database is overload, e.g. via [exceeding the connection limit](#database-connection-limit) | 
| MATCHING-DATA-REFRESH-DB-SIZE-ACTIVE | DATA-REFRESH-FUNCTION | DataRefresh-ActiveDatabaseSize | Determines the size (in scale, not bytes) of the active matching database. Must allow `MATCHING-DATABASE-MAX-SIZE`. | 
| REPEAT-SEARCH-MATCHING-MAX-CONCURRENT-PROCESSES-PER-INSTANCE | ATLAS-REPEAT-SEARCH-FUNCTION |AzureFunctionsJobHost--extensions--serviceBus--messageHandlerOptions--maxConcurrentCalls| Same as the similar setting above, but for the repeat search module. Note that when repeat searches are run concurrently with first time searches, the database should be able to handle the max concurrent load of *both* apps.  |
| REPEAT-SEARCH-MATCHING-MAX-SCALE-OUT | ATLAS-REPEAT-SEARCH-FUNCTION | WEBSITE-MAX-DYNAMIC-APPLICATION-SCALE-OUT | Same as the similar setting above, but for the repeat search module. Note that when repeat searches are run concurrently with first time searches, the database should be able to handle the max concurrent load of *both* apps. |

### Match Prediction Algorithm

| Terraform Setting          | Functions App Name                 | Functions App Setting Name | Description                |
| -------------------------- | ------------------                 | -------------------------- | -----------                | 
| MATCH-PREDICTION-ACTIVE-HF-SET-CACHE-EXPIRY-MINUTES | ATLAS-MATCH-PREDICTION-FUNCTION | HaplotypeFrequencySetCache:ActiveSetCacheExpiryMinutes | Absolute cache TTL, in minutes, for the active `HaplotypeFrequencySets` lookup used when selecting donor/patient frequency sets. |
| MATCH-PREDICTION-WORKER-MAX-CACHED-FREQUENCY-SETS, MATCH-PREDICTION-WORKER-SET-CACHE-EXPIRY-MINUTES | match-pred-ca (Container App) | HaplotypeFrequencySetCache__MaxCachedFrequencySets, HaplotypeFrequencySetCache__SetCacheExpiryMinutes | The match prediction worker's own frequency set cache: 30 sets, for a day. Separate from MATCH-PREDICTION-MAX-CACHED-FREQUENCY-SETS / MATCH-PREDICTION-SET-CACHE-EXPIRY-MINUTES (10 sets, 60 minutes, used by the other hosts), because a search load touches many sets: with 10, DEV searches reloaded each set about 3 times per replica (ATL-476). Check the GC heap in the `Match prediction worker runtime` event (ATL-474) before raising it further: the GC heap limit is 75 % of the container's memory. |
| MAX-CONCURRENT-ACTIVITY-FUNCTIONS | ATLAS-FUNCTIONS | AzureFunctionsJobHost--extensions--durableTask--maxConcurrentActivityFunctions | *Per instance*, dictates the number of parallel match prediction batches that can be run. It is recommended that this stay very small (ideally 1), as match prediction is a very memory intensive process - and allowing more concurrent processes significantly increases the risk of hitting out of memory exceptions, causing match prediction to slow down significantly (or worse, never finish) |
| ORCHESTRATION-MATCH-PREDICTION-BATCH-SIZE | ATLAS-FUNCTIONS | AtlasFunction-Orchestration-MatchPredictionBatchSize | The number of donors that will be processed in each batch of match prediction. Should be kept reasonably low, to engender horizontal scaling, and to reduce risk of running out of memory. When reduced too much, we may see that orchestration overhead starts to outweigh the benefits of engendering more horizontal scaling, and searches may start to slow down a bit overall. |

### Search Results

| Terraform Setting          | Functions App Name  | Functions App Setting Name  | Description  |
| -------------------------- | ------------------  | --------------------------  | ------------ |
| RESULTS_BATCH_SIZE | ATLAS-FUNCTIONS, ATLAS-MATCHING-ALGORITHM-FUNCTIONS, ATLAS-REPEAT-SEARCH-FUNCTION | AzureStorage-SearchResultsBatchSize | Batch size (number of results written per file) for saving search/matching results |
| SEARCH_RESULTS_READY_SUBSCRIPTION_NAMES| N/A | N/A | Subscription names for the search-results-ready and repeat-search-results-ready Service Bus topics (in addition to Audit subscriptions). If not provided, no additional subscriptions will be created. |

## Search Concurrency Configuration

There are a few different factors to affecting the concurrency of search operations. These may need tweaking on a per-installation basis, so a summary of the various options is available here.

### Number of concurrent processes

This is a multi-faceted configuration option due to horizontal scaling.

First we must consider how many concurrent processes can run on each worker.

This is configured by `maxConcurrentCalls` for regular functions (e.g. matching), and `maxConcurrentActivityFunctions` for durable functions (e.g. match prediction).

Next we must consider how many horizontal instances can be spun out.

This is configured by the minimum of `WEBSITE_MAX_DYNAMIC_SCALE_OUT` (sets it per-functions app), and `maximum elastic worker count` (sets it per service plan).
These can both be set via terraform configuration values.

#### Repeat Search

Two different functions apps will be making calls to the matching database - the matching functions app (running initial searches), and the repeat search app (running repeat searches).

The vast majority of the time, repeat searches are expected to be much quicker and less resource intensive, so can afford lower concurrency.

The above considerations of (scaled instances * max concurrent calls) applies for both functions apps - so the database connection limit in practice is:

`(<concurrent matching calls per instance> * <number of matching instances>) + (<concurrent repeat matching calls per instance> * <number of repeat search instances>)`


### Database Connection Limit

Within a single process, multiple database connections may be concurrently opened.

There is a hard limit on concurrent connections set by Azure - docs available for [vCore](https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-vcore-single-databases)
and [DTU](https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-dtu-single-databases) pricing models.

If the concurrency settings of Atlas searches involve more concurrent connections than the provisioned database tier, requests will fail. Ensure that the database provisioned is large enough to
handle the required number of concurrent connections.
