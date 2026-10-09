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

### Manual Trigger

Atlas can be configured to automatically re-run the data refresh process as soon as it detects a new HLA nomenclature version. There are some cases when it may be preferable to run the job manually: 

(a) If the same nomenclature version is re-uploaded to the source, e.g. to fix any errors
(b) If this installation of Atlas has opted to disable the auto-run refresh - in this case manual will be the only way to trigger this job
    - An example reason to maintain manual control would be to ensure that haplotype frequency nomenclature and matching nomenclature are updated simultaneously
    
To manually trigger the Job, call the `SubmitDataRefreshRequestManual` HTTP Azure function, on the Data Refresh Functions App (`DATA-REFRESH-FUNCTION`).
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

##### HLA nomenclature version and HLA Metadata Dictionary snapshot

Each run records two values in `[MatchingAlgorithmPersistent].[DataRefreshHistory]`:

- `HlaNomenclatureVersion`: the HLA nomenclature version of the run. It is read from WMDA once, and recorded before
  the HLA Metadata Dictionary is recreated.
- `HlaMetadataDictionarySnapshotUtc`: the time, in UTC, that identifies the HLA Metadata Dictionary tables the run
  created. Every table of the run ends its name with this time, in the format `yyyyMMddHHmmssfff`. It is recorded when
  the `MetadataDictionaryRefresh` stage completes. It is empty for runs from before this value was recorded.

A continued run keeps both values. It does not read the version from WMDA again, even if WMDA has a newer version.
If the run stopped while the dictionary was being recreated, the next attempt recreates it again under the recorded
version, and records the snapshot of that last recreation.

##### Stale leases, and how they clear themselves

Only one invocation may process a refresh record at a time, enforced by a lease held on the record. A replayed
message is refused while that lease is held, and completes without running any stage. If the server died, the
lease was never released, so it remains valid for up to `DataRefresh:LeaseDurationMinutes` (30 by default)
after the last renewal.

**This recovers on its own, and normally needs no intervention.** The `RecoverStalledDataRefreshes` timer
function sweeps for records that are still open, hold no live lease, and have shown no sign of life for
`DataRefresh:WatchdogGraceDurationMinutes` (60 by default), and publishes a fresh request for each. Whichever
invocation then claims the record resumes it from its last completed stage. At the default settings a record
abandoned by a dead worker is picked back up within roughly 90 to 105 minutes of its last lease renewal: the
30 minute lease expiry, plus the 60 minute grace, plus up to one sweep interval
(`DataRefresh:WatchdogCronSchedule`, every 15 minutes by default).

It also covers the two cases a redelivery cannot. A replay refused by the lease is *consumed* - completed, not
dead-lettered - so there may be nothing left to replay; and a request message can be lost before it is ever
delivered. Neither leaves a live lease behind, so both look the same to the sweep as any other stall.

Each recovery logs an Application Insights **event** named `Data refresh stall auto-recovered`, carrying the
record id. Nothing else emits it, so it is worth alerting on: a record recovered repeatedly is failing for some
reason the retry cannot fix, and wants investigating rather than leaving to the sweep.

##### Intervening before the watchdog does

Only needed if you cannot wait out the grace period, or if the watchdog itself is not running. To check the
latter, look at the `RecoverStalledDataRefreshes` function's own invocation and exception telemetry in
Application Insights. Do not go by the recovery event above: it fires only when a stall is actually found, so
its absence is the ordinary healthy state and says nothing about whether the sweep is running.

Publish a request to the `data-refresh-requests` topic yourself:

```json
{ "DataRefreshRecordId": <record id> }
```

The job then continues from its last completed stage, unlike (b), which marks the record failed so that the
next refresh starts from scratch.

If the record still holds a live lease, that request will be refused, and - **only if you are certain the
original process is gone** - the lease has to be cleared first:

```sql
UPDATE [MatchingAlgorithmPersistent].[DataRefreshHistory]
SET LeaseOwner = NULL, LeaseExpiresUtc = NULL
WHERE Id = <record id>
```

Clearing a live lease lets two refreshes run against one record and the same transient database, each
independently deciding which stages to skip.

Note that clearing the lease alone does not hand the record to the watchdog any sooner: the sweep also requires
the record to have been idle for the whole grace period, measured from `RefreshLastContinuedUtc`. Publishing the
request is what recovers it immediately.

#### (b) Manual cleanup
 
If you prefer not to continue a refresh, any live request messages must be purged from the `matching-algorithm` subscription, and teardown performed.
Teardown can either be done entirely manually, or the `RunDataRefreshCleanup` function (on the Data Refresh Functions App) can be run, which performs the described steps.
 
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
  - The donor genotype precomputation run of the record must be cancelled, so that the workers stop.
    - `RunDataRefreshCleanup` does this. If you clean up by hand, use the SQL in [When the refresh fails](#when-the-refresh-fails).

### Donor genotype precomputation (stage 65)

Stage 65 stores the imputed genotype sets of all donors. It sends the work to the precomputation workers, and waits
until they have done it. On a full donor set, the stage can run for many hours. For how the stage works and its
settings, see [the Matching Algorithm README](README_MatchingAlgorithm.md#donor-genotype-precomputation-data-refresh-stage-65).

#### Normal progress

- The stage, the timers and the workers log to Application Insights with the prefix `DONOR GENOTYPE PRECOMPUTATION:`.
- After the build, one trace gives the count of donors, groups and batches, and the time of each build step.
- While the stage waits, it logs the count of batches in each status every `PollIntervalSeconds`, for example
  `1200 of 4000 batches done`. This count must go up.
- The workers send metrics for each batch: `DonorGenotypePrecomputation.BatchDurationMs`,
  `DonorGenotypePrecomputation.BatchComputedGroupCount` and `DonorGenotypePrecomputation.BatchFrequencySetCount`
  (`customMetrics | where name startswith "DonorGenotypePrecomputation."`).
- The number of worker replicas follows the count of messages on the `precomputation-worker` subscription.
- When the stage is complete, the column `DonorGenotypePrecomputationCompleted` of the refresh record gets a value.

#### Alert: "Data refresh donor genotype precomputation has stalled"

High priority. No batch has finished for `StallAlertMinutes`, and the data refresh still waits. The stage sends one
alert for each stall. When a batch finishes again, it logs `Batches of run ... finish again`.

Do these checks:

1. Make sure that the workers run. Look at the replicas of the worker Container App and at their logs. A worker reads
   the open refresh record when it starts. It does not start if no record, or more than one record, is open.
2. Look at the `precomputation-worker` subscription:
   - Messages wait, but no worker takes them: the problem is in the workers.
   - No message waits, and batches stay `Requested`: the messages are lost. See [A lost message](#a-lost-message).
3. Look at the dead-letter queue of the subscription. See [The dead-letter queue](#the-dead-letter-queue).
4. Make sure that the timer functions of the Data Refresh app run: `FinaliseCompletedPrecomputationRuns`,
   `MarkAbandonedPrecomputationBatches` and `RequeueFailedPrecomputationBatches`. Look at their invocations in
   Application Insights. If every batch is `ResultsReceived` or `PermanentlyFailed`, but the run is still `Running`,
   the finalise timer does not run.
5. Batches that stay `InProgress`: a worker that stopped keeps its batch until the lease expires
   (`PrecomputeWorker:BatchLeaseMinutes`, 60 by default). Then the timers send the batch again.

Use the [SQL for support](#sql-for-support) to see the batches.

#### Alert: "Data refresh donor genotype precomputation failed for N donor(s)"

The run is complete, and some donors have no stored genotype set for one or more combinations of loci. **The data
refresh continued.** The alert gives the counts, the threshold (`DataRefresh:Precompute:MaxFailedDonorFraction`), and
up to 10 sample errors: the first failure of each different message.

- Medium priority: the failed donors are at or below the threshold.
- High priority: the failed donors are above the threshold. Find the cause before the next refresh.

Read the sample errors. Usual causes:

- An HLA typing that the HLA Metadata Dictionary does not accept. Stage 50 usually reports the same donors in its own
  failed-donors alert.
- No haplotype frequency set for the registry and ethnicity, and no global set.
- An outage that lasted for all the retries of a batch. These batches are `PermanentlyFailed`.

The stage removes the groups and their donors (the staging data) after it sends this alert. So the failed groups and
the failed donors of the [SQL for support](#sql-for-support) are available only while the stage runs. After that, find
the donors that have no stored set with the last query of that section.

The refresh does not compute these donors again. An update of a donor computes its sets again, and so does the next
full data refresh.

#### SQL for support

Run the persistent database query first. Run the other queries on the transient database of the refresh record: the
`Database` column gives `DatabaseA` or `DatabaseB`.

```sql
-- Persistent database: the open refresh record, and the stages that it has completed.
SELECT Id, [Database], HlaNomenclatureVersion, RefreshRequestedUtc, RefreshLastContinuedUtc,
       IndexRecreationCompleted, DonorGenotypePrecomputationCompleted, DatabaseScalingTearDownCompleted
FROM [MatchingAlgorithmPersistent].[DataRefreshHistory]
WHERE RefreshEndUtc IS NULL
```

```sql
-- The run of the record. The stage still waits while the record is open, DonorGenotypePrecomputationCompleted is
-- NULL, and the run is Building or Running. Building: the build runs. Running: the workers compute the batches.
SELECT Id, Status, TotalDonorCount, TotalGroupCount, TotalBatchCount, GroupsPerBatch, CreatedUtc, StatusDateUtc, CompletedUtc
FROM DonorGenotypePrecomputationRuns
WHERE DataRefreshRecordId = <record id>
```

```sql
-- The batches in each status.
SELECT Status, COUNT(*) AS Batches, SUM(FailedGroupCount) AS FailedGroups, MIN(StatusDateUtc) AS OldestStatusDateUtc
FROM DonorGenotypePrecomputationBatches
WHERE RunId = <run id>
GROUP BY Status
```

```sql
-- The batches that failed, or that have failed groups. FailureException has the full error.
SELECT Id, BatchNumber, Status, RetryCount, FailedGroupCount, FailureMessage, LeaseExpiresUtc, DispatchedUtc, StatusDateUtc
FROM DonorGenotypePrecomputationBatches
WHERE RunId = <run id>
  AND (Status IN ('Failed', 'Abandoned', 'PermanentlyFailed') OR FailedGroupCount > 0)
ORDER BY Id
```

```sql
-- The failed groups of the batches with results. RepresentativeDonorId is the DonorId of the donor that the worker used.
SELECT b.Id AS BatchId, g.Id AS GroupId, g.AllowedLociKey, g.RepresentativeDonorId, g.DonorCount, g.FailureMessage
FROM DonorGenotypePrecomputationBatches b
INNER JOIN DonorGenotypePrecomputationGroups g ON g.Id BETWEEN b.FirstGroupId AND b.LastGroupId
WHERE b.RunId = <run id>
  AND b.Status = 'ResultsReceived'
  AND b.FailedGroupCount > 0
  AND g.RunId = <run id>
  AND g.SubjectGenotypeSetValueId IS NULL
```

```sql
-- The failed donors, against the threshold. This is the count of the failure alert: all the donors of a permanently
-- failed batch, and the donors of the failed groups of a batch with results. Each donor counts one time.
WITH FailedGroups AS (
    SELECT g.Id AS GroupId
    FROM DonorGenotypePrecomputationBatches b
    INNER JOIN DonorGenotypePrecomputationGroups g ON g.Id BETWEEN b.FirstGroupId AND b.LastGroupId
    WHERE b.RunId = <run id>
      AND g.RunId = <run id>
      AND (b.Status = 'PermanentlyFailed'
           OR (b.Status = 'ResultsReceived' AND b.FailedGroupCount > 0 AND g.SubjectGenotypeSetValueId IS NULL))
),
FailedDonors AS (
    SELECT COUNT(DISTINCT gd.DonorId) AS FailedDonorCount
    FROM FailedGroups fg
    INNER JOIN DonorGenotypePrecomputationGroupDonors gd ON gd.GroupId = fg.GroupId
)
SELECT fd.FailedDonorCount, r.TotalDonorCount,
       CAST(fd.FailedDonorCount AS float) / NULLIF(r.TotalDonorCount, 0) AS FailedDonorFraction
FROM DonorGenotypePrecomputationRuns r
CROSS JOIN FailedDonors fd
WHERE r.Id = <run id>
```

```sql
-- After the stage: the donors that have no stored set for one or more of the four combinations of loci. This reads
-- every donor, so it can take some minutes on a full donor set.
SELECT d.DonorId
FROM Donors d
WHERE (SELECT COUNT(*) FROM DonorSubjectGenotypeSets s WHERE s.DonorId = d.DonorId) < 4
```

#### A lost message

A batch is `Requested` when its message is on the topic. No timer moves a `Requested` batch, so a batch whose message
is gone stops the stage. The stall alert then tells you.

A message is lost when all of these are true:

- Batches of the run stay `Requested`.
- The active message count of the `precomputation-worker` subscription is 0. (The count includes the messages that a
  worker holds.)
- The dead-letter queue of the subscription is empty.

Then mark these batches as abandoned. The requeue timer sends each batch again, or marks it as permanently failed when
it has no retries left.

```sql
-- Transient database of the refresh record.
UPDATE DonorGenotypePrecomputationBatches
SET Status = 'Abandoned', FailureMessage = 'The message was lost. Abandoned by support.', StatusDateUtc = SYSUTCDATETIME()
WHERE RunId = <run id> AND Status = 'Requested'
```

#### The dead-letter queue

The `AbandonDeadLetteredPrecomputationBatches` function of the Data Refresh app reads the dead-letter queue of the
`precomputation-worker` subscription. For each message, it marks the batch as abandoned, and the requeue timer sends
the batch again. So the dead-letter queue is normally empty.

- A message that names no batch is removed, and the function logs the error
  `A dead-lettered batch message names no batch`. That batch stays `Requested`: see [A lost message](#a-lost-message).
- Messages that stay in the dead-letter queue show that the function does not run. Look at its invocations and errors
  in Application Insights.

#### When the refresh fails

When the refresh fails, it cancels the precomputation run (status `Cancelled`) and removes the staging data. The
workers then skip the messages that are left on the subscription. `RunDataRefreshCleanup` does the same for each
record that it closes.

- A temporary database error while the stage waits does not stop the stage. The stage logs the warning
  `could not read run`, and reads the run again at the next poll. When the polls fail for
  `DataRefresh:Precompute:StallAlertMinutes`, the stage fails.
- After a SQL error, the refresh is not closed. The refresh scales the database down to the dormant size, and Service
  Bus delivers the refresh request again. Stage 30 then scales the database up, and stage 65 continues its run. While
  the database scales, batches can fail. The requeue timer sends them again.
- If the cancel fails, the Data Refresh app logs the error `could not be cancelled`. Then the workers can continue to
  compute the batches of the failed refresh. Cancel the run by hand, on the transient database of the record:

```sql
UPDATE DonorGenotypePrecomputationRuns
SET Status = 'Cancelled', StatusDateUtc = SYSUTCDATETIME()
WHERE DataRefreshRecordId = <record id> AND Status IN ('Building', 'Running');

TRUNCATE TABLE DonorGenotypePrecomputationGroupDonors;
TRUNCATE TABLE DonorGenotypePrecomputationGroups;
```

A cancelled run cannot continue. Request a new data refresh. Its data deletion stage removes all the precomputation
tables of the database.

#### When the refresh invocation ends during stage 65

The `functionTimeout` of the Data Refresh app (`host.json`) is 2 days, for the whole refresh. A release, a restart or a
platform event can also end the invocation. Stage 65 can continue after this:

1. The lease of the record expires, and the watchdog requests the refresh again. See
   [Stale leases, and how they clear themselves](#stale-leases-and-how-they-clear-themselves). This takes about 90 to
   105 minutes.
2. Stage 30 runs again. The database is still at the refresh size, so it does not change.
3. Stage 65 continues from its run. It does not build again. It sends the pending batches, and waits again.

The workers and the timers continue while no invocation runs, so little work is lost.


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

### Re-publish failed `updated-searchable-donors` messages
After completion of donor import, any detected donor changes are published as messages to the `updated-searchable-donors` topic, for consumption by the matching algorithm donor management functions.
They process the updates in batches and apply them to the active matching algorothm database, so that the changes will be visible to search requests.

Sometimes an update may fail to be applied when it should have been.
A real world example: the donor HLA contains a very new MAC which has not yet made it to the MAC dictionary, resulting in a HLA expansion failure.
In such a case, after updating the MAC dictionary, invoke the http endpoint `ManuallyPublishDonorUpdatesByDonorId` on `<ENV>-ATLAS-DONOR-IMPORT-FUNCTIONS` with the Atlas donor ID in the request body (wrap one or more IDs in an `int[]`).
This will re-publish an update for the affected donor.

Note, this function publishes a brand new update message containing the latest data held for the donor, as the data within the original failed update may be out of date.
If the donor has been deleted from the donor store since the original update failure, then the new message will reflect that to ensure the donor is no longer searchable.