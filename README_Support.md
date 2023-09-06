# Support

This README contains useful information for supporting the ATLAS system, and some troubleshooting steps for common problems.

## Notifications 

Two types of notifications are sent from ATLAS, and should be routed to appropriate communication channels as part of ATLAS setup (see the [integration README](README_Integration.md))

- Notifications

These contain potentially useful information, such as details on successful import processes.
 
- Alerts

These contain information that should be actioned - e.g. failures in import processes. 

We recommend routing these to different channels, so that alerts can always be acted upon.


## Haplotype Frequency Upload

### HLA Metadata Dictionary lookup errors

The haplotype frequency set (HFS) import process involves validating and converting the provided HLA data.

> Is lookup failing for the first allele in the file, or import failing with a dictionary error?

If the failed lookup is the first typing listed in the uploaded HFS file, or if the error states "The given key 'A' was not present in the dictionary" (or some other dictionary related error), it is likely that the **HLA Metadata Dictionary** (HMD) has not been generated for the given HLA nomenclature version (or something went wrong during HMD creation). Check the [`nomenclatureVersion` used in the file](/Schemas/HFSetSchema.json#5), and trigger recreation of the HMD using the endpoint [`RefreshHlaMetadataDictionaryToSpecificVersion` on the matching-algorithm functions app](/Atlas.MatchingAlgorithm.Functions/Functions/HlaMetadataDictionaryFunctions.cs#47).

Atlas components have an in-memory cache of the HMD which lasts ~24 hours. Restart the relevant functions app (in the case of HFS import, the match prediction app) to ensure the new version of the HMD is used.

// TODO: #775: Improve error messaging in this scenario

> Is the typing in the HFS a valid allele and also part of a G-Group?

ATLAS only supports haplotype frequency data encoded as G-Groups ("large", e.g., `01:01:01G` or "small", e.g., `01:01g`). The import will fail if the file contains an allele name that is actually part of a wider G group. E.g., if a "large" G group HFS file contains the allele `A*01:01:01:01`, the file will be rejected because the allele is part of `A*01:01:01G`. Another example for "small" G group is the allele `A*43:02N` which should be submitted as the g group named, `43:01`.

In this case, the local function, `TransformHaplotypeFrequencySet`, can be used to correct the HFS. [See this README for further details](/README_ManualTesting.md/#transform-a-haplotype-frequency-set-file-to-findreplace-an-invalid-typing).


### Rolling back an upload

Haplotype Frequency sets are soft-deleted as part of the import process. This means that in the case of an issue with the latest set for a registry/ethnicity, it is possible to 
very quickly roll back to a previous version.

This is not automated - it will require manually changing the SQL database. The active set in question will need to have its `Active` column set to 0, and the desired replacement set to `1`. 
These two steps must happen in order, as only one set can be active (per ethnicity/registry pair) at a time.

Alternatively, an older file can be re-uploaded, which will automatically become active if successful. This does not require database access, but does require access to the upload file
for the desired rollback, and will take longer than the manual SQL approach - especially for large sets (order of minutes).   


## Data Refresh 

## Manual Trigger

Atlas can be configured to automatically re-run the data refresh process as soon as it detects a new HLA nomenclature version. There are some cases when it may be preferable to run the job manually: 

(a) If the same nomenclature version is re-uploaded to the source, e.g. to fix any errors
(b) If this installation of Atlas has opted to disable the auto-run refresh - in this case manual will be the only way to trigger this job
    - An example reason to maintain manual control would be to ensure that haplotype frequency nomenclature and matching nomenclature are updated simultaneously
    
To manually trigger the Job, call the `SubmitDataRefreshRequestManual` HTTP Azure function, on the Matching Algorithm Functions App.
Configuration options are available as per the model `DataRefreshRequest`

### In the case of the refresh job server dying

The data refresh function is set up such that if it *fails* for any reason, the algorithm/infrastructure will be left in a reasonable state.

If, however, the server were to stop mid-job, automatic teardown would not be applied - in particular, the database would be left at a more expensive tier than is required. We can describe such a refresh as "stalled"

(The reasons for this include: a release of ATLAS, a manual restart of the service running the refresh, Azure dropping the worker running the refresh e.g. due to a power failure, or unavoidable maintenance.)

In this case there are two options:

#### (a) Continued Refresh

Refresh requests are managed via the service-bus topic: `data-refresh-requests`.
The automatic replay of a live message - or the manual replay of a dead-lettered message - will lead to the continuation of an incomplete job.

Check Application Insights for continuation progress or exceptions.

If request replay fails and Application Insights shows the exception: "Exception while executing function: RunDataRefresh [...] There is no record of an initiated job. Please submit a new data refresh request.",
then find the record for your data refresh attempt with the shared db table, `[MatchingAlgorithmPersistent].[DataRefreshHistory]` and set value of `[RefreshLastContinuedUtc]` to `NULL`.
This will allow the job to continue from where it left off.

#### (b) Manual cleanup
 
If you prefer not to continue a refresh, any live request messages must be purged from the `matching-algorithm` subscription, and teardown performed.
Teardown can either be done entirely manually, or the `RunDataRefreshCleanup` function can be run, which performs the described steps.
 
If a refresh stalls locally, you can likely ignore the infrastructure part of this checklist.

- Azure Infrastructure
  - **URGENT** - if the database was scaled up to the refresh level, it will need to be manually scaled back down. This should be done as soon as possible, as the refresh size is likely to have very high operating costs
  - The Donor Import functions may need to be turned back on
  - This is encapsulated within the "RunDataRefreshCleanup" function - which can be triggered rather than manually changing infrastructure if preferred
- Search Algorithm Database
  - The transient database should be mostly safe to ignore - live searches will not move to the database that failed the refresh, and beginning a new refresh will wipe the data to begin again
  - Indexes may need manually adding to the hla tables, if the job crashed between dropping indexes and recreating, it may fail on future runs until they are re-added
  - The latest entry in the `DataRefreshHistory` table will not be marked as complete, and no future refresh jobs will run. It should be manually marked as failed, and an end date added.
    - If the `RunDataRefreshCleanup` function was used for infrastructure cleanup, this will have been covered by that function


## Search

### Search error - "donor id not found"

The matching algorithm uses internal donor ids for search, and will look up external donor codes on completion of a search request. It assumes that all donors matched will be present in the master Atlas donor store.

If this assumption is broken, an exception will be thrown and the search will fail. (This will manifest as a `KeyNotFoundException` in [SearchService.cs](Atlas.MatchingAlgorithm/Services/Search/SearchService.cs)) 

There is an edge case in which this situation is possible - if a donor is removed from the Atlas system, and shows up in a search result set before the matching algorithm has applied the deletion to its donor store - 
this window is configurable, but expected to be in the order of a low number of minutes.

Automatic retry logic should take care of this edge case, but if a search repeatedly fails with such an error:

- If the search was very quick, wait a few minutes to give the matching donor processor a chance to apply the deletion
- If the issue still persists, it implies that the master donor store and the matching store have drifted out of sync. This is not expected and will require investigation.

### Searches not returning results

Searches should post a notification to the `search-results-ready` topic - either for success, containing information about the results, or notifying of an algorithm failure. 

If no notification is received, either: 

- The search is still running

Check the `matching-results-ready` topic's `audit` subscription to see if the matching component finished - matching is expected to be much quicker than match prediction.

Query application insights for logs relating to the search request in question - both Matching and Match Prediction log a customDimension `SearchRequestId` to quickly identify all logs for a given search.
If logs are still actively being written, the search is likely just a very long-running one.

- The search failed, but no notification was received

In theory this is not possible, but it could happen due to either a bug in ATLAS, or a failure in the Azure infrastructure running the application. 
 
Check the Application Insights logs for the search request id, looking out for any `Exceptions` in particular. 

### Matching Algorithm running out of memory

For particularly large searches, some test cases have caused OOM exceptions on deployed hardware. If this happens, first determine whether the solution has been configured appropriately on your 
environment - if it is still deemed to have too high memory usage, development investigation may be needed. The configuration options that affect memory usage are: 

- Service plan
    - The smallest available elastic service plan is an EP1 elastic plan. This should be enough for most search use cases, but if other configuration options lead to high memory usage, scaling up to a higher 
    plan is an easy step to take to prevent OOM errors.
- Number of allowed invocations per instance
    - As the elastic plan scales out to multiple instances, each instance will be allowed more memory as per the service plan (and charged accordingly), so this is not a concern for memory
    - Each instance has a certain number of allowed concurrent processes - the more parallel searches per instance, the higher the memory usage.
    - This can be configured in [the relevant host.json](Atlas.MatchingAlgorithm.Functions/host.json)
- The matching batch size
    - This determines the size of batches used internally in the matching process. Broadly speaking, a higher batch size leads to faster searches, but higher memory usage
    
Testing on a search with ~20,000 donor results (on a dataset of ~30M donors) indicates that, with a batch size of 250,000, a single search uses a peak of *roughly* 1.2GB. Of this, ~400Mb are used for cached reference data, 
and will therefore be a constant baseline rather than scaling with concurrent searches. This test case is expected to be a reasonable "worst-case" scenario for memory usage, in the a 30M donor test environment.   

### Matching requests appear to not be processing

The matching algorithm has a high SQL timeout, as it's expected that expensive searches under high load can take a while to run.

In the case of some transient database failures, connections will not be closed, and this timeout will be hit before the search fails. (One example of this is if the provisioned database is too small for the concurrent searches
run on it. If this is the case, either the provisioned database must be increased in processing power, or the number of concurrent searches should be reduced - [see settings above for how to change this](#matching-algorithm-running-out-of-memory))
 
This problem may manifest itself as follows:

- A single search appears to have hung and is not read from the matching queue
    - This will be the case if Atlas was not running at capacity when the transient failure occured.
    - Without manual changes, the search will restart after the SQL timeout expires (approx. one hour)
    - To expedite this, restart the matching algorithm function
- No searches are read from the matching queue
    - This will be the case if Atlas was running at capacity when the transient failure occured.
    - Without manual changes, no searches will be processed until the SQL timeout expires (approx. one hour)
    - To expedite this, restart the matching algorithm function

### Search is not completing

If a search has not yet returned results after a reasonable period of time, there are some steps that can be taken to track this "missing" search:

* Matching Algorithm
  * Is the search still ongoing in the matching algorithm? 
    * Check the `matching-requests` topic, in the `matching-algorithm` subscription. There are three possible states for your search: 
      * (i) Still processing: the message initiating your search request is still in the subscription. This indicates matching is still ongoing for your searches. 
        * All searches are expected to run in a low number of minutes - if it has been significantly longer than that, you may be running on a database tier that is too low for your data volume. 
      * (ii) Dead lettered: the message will be found in the dead letter queue. This means the search failed x times, where x is the retry count (default is 10 attempts). 
        * You should be able to identify the failure reason from the matching results message if present, and AI logs if not.
      * (iii) Complete: the message is no longer in the subscription, nor the dead-letter queue. 
  * Was the request scheduled for match prediction? 
    * Check the `matching-results-ready` topic, in the `audit` subscription.
    * If your search is not in this subscription, it was never queued for match prediction - and thus is not ever expected to have results in the `search-results-ready` topic / `atlas-search-results` blob container. 
    * For searches run without match prediction, the matching results are expected to be used directly - i.e. the `matching-results-ready` topic / `matching-algorith-results` blob container.
  * Is the request still scheduled for match prediction orchestration? 
    * If your search is still in the `matching-results-ready` topic's `match-prediction-orchestration` subscription, this implies that the `Atlas` functions app is failing to initiate match prediction
    * Note that messages leaving this subscription do *NOT* indicate that they have completed match prediction - just that match prediction has begun
  * Is match prediction still ongoing?
    * We have a few options available to help track match prediction:
      * Durable Functions Storage
        * Match Prediction horizontal scaling is managed by the Azure Durable Functions framework - we can look at the azure storage account used to drive this process, but cannot guarantee that this structure will stay the same with future updates
          * In the `<env>atlasdurablefunc` storage account: 
            * `AzureFunctionsHubInstances` will contain a row per orchestrator function, with a `RunTimeStatus` - this status can be used to identify orchestrator functions that are still in progress
            * In the `atlasfunctionshub-workitems` queue, any items in this queue indicate work is still being performed by durable functions
      * Results Files
        * Each donor will have a unique match prediction output, written to `<env>atlasstorage/match-prediction-results`. 
        * The matching algorithm output message will contain a number of donors matched in the search
        * You can compare the number of donors expected to be returned, to the number of results files in the folder for your search request
        * If the number of results files is increasing over time, this indicates match prediction is still ongoing
      * AI Logs
        * Match prediction will log, as `traces`, multiple stages of the process, in addition to the trace logs written by the functions framework itself
    * If it appears that the match prediction functions app is continuously starting batches of donors, but never finishing them, it is possible that the functions app is running on a service plan without enough memory to handle the prediction work (this will be 
    more likely in scenarios with very ambiguous patients/donors, or if the match prediction _batch size_ or _number of concurrent activity functions per instance_ are set too high.)
      * If you're seeing such behaviour, try: 
        * (a) reducing the number of concurrent activity functions
        * (b) increasing the amount of memory available by scaling up the app service plan

### Search logs

[Log files](/Atlas.Client.Models/Search/Results/SearchLogs.cs) named `{search-request-id}-log.json` are uploaded to `matching-algorithm-results` and `atlas-search-results` containers. 

## MACs

### New MAC is showing as unrecognised

MACs are imported nightly - if a search is run for a brand new MAC less than a day after it was published, the MAC store will not be up to date, and a lookup error will occur. 

If MAC lookup fails:

- If the MAC is not valid
   
The exception is expected.

- If the MAC is valid, and <24 hours old

The MAC has not yet been imported. Manually trigger an early MAC import, or wait 24 hours.

(To manually trigger an import, call the `ManuallyImportMacs` function - either from a REST client of your choice, including the function key from Azure as an authorisation header, or directly from the Azure Portal's "Funtions" view, which allows you to execute individual functions directly from the portal)

If this happens frequently, consider increasing the frequency of the MAC import.

- If the MAC is valid, and >24 hours old

Check the MAC store in Azure storage, to see if it has been imported. 

If not, the import may be failing - check the Atlas Functions app logs in Azure Portal / Application Insights

### MAC Import is failing

Check exception details from the MAC import jon in AI. 

If the error mentions "The specified entity already exists." - check the following data in the `<env>atlasstorage/AtlasMultipleAlleleCodes` table in Azure Table Storage: 

(a) Record with `Partition=Metadata` and `Row=LastImported` - this is the logged last imported MAC
(b) Query the same table to see if this is correct - use the partition key of the length of the last seen MAC, and a >= operator to see if any later MACs exist. 

If the MAC import catastrophically fails between inserting new MACs and updating the "last seen" MAC (e.g. due to a platform failure), these can stray out of sync. 

In this case, identify the *actual* last imported MAC manually, and update the "LastUpdated" row to be correct. From this point onwards the import job should work as expected. 

## Donor Import

### Donors are failed to import

All donors that didn't pass validation are logged not only to AI, but also to a database table where could be queried ([see donor import README](/README_DonorImport.md/#file-validation)).