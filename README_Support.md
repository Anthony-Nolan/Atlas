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

### Allele lookup error

The haplotype frequency import process involves validating and converting the provided hla data.

> Is lookup failing for the first allele in the file?

If the failed lookup is a very common allele e.g. `01:01:01G` - and is the first one in the uploaded file - it is likely that th **HLA Metadata Dictionary** has not been generated for
the given nomenclature version. Check the version used in teh file, and trigger a Metadata refresh.

Both algorithms have an in-memory cache of the metadata dictionary, which lasts ~24 hours. To ensure the new version of the HMD is used, the algorithm functions apps will need restarting.

// TODO: ATLAS-727: Improve error messaging in this scenario

> Is the allele valid, and a G-Group?

ATLAS only supports haplotype frequency data as G-Groups. This means that if you provide an allele that is valid, and represented by a G-Group (and was perhaps a G-Group of one in a 
previous nomenclature version) - the file will be rejected.


### Rolling back an upload

Haplotype Frequency sets are soft-deleted as part of the import process. This means that in the case of an issue with the latest set for a registry/ethnicity, it is possible to 
very quickly roll back to a previous version.

This is not automated - it will require manually changing the SQL database. The active set in question will need to have its `Active` column set to 0, and the desired replacement set to `1`. 
These two steps must happen in order, as only one set can be active (per ethnicity/registry pair) at a time.

Alternatively, an older file can be re-uploaded, which will automatically become active if successful. This does not require database access, but does require access to the upload file
for the desired rollback, and will take longer than the manual SQL approach - especially for large sets (order of minutes).   


### Deleting old sets

As there is no hard delete in the upload process, no sets will ever be deleted during ongoing operation. 

It may be desirable to manually delete older sets, to free up database space and keep database operations quick.


## Data Refresh 

### In the case of the refresh job server dying

The data refresh function is set up such that if it *fails* for any reason, the algorithm/infrastructure will be left in a reasonable state.

If, however, the server were to stop mid-job, automatic teardown would not be applied - in particular, the database would be left at a more expensive tier than is required.

(The reasons for this include: a release of ATLAS, a manual restart of the service running the refresh, Azure dropping the worker running the refresh e.g. due to a power failure, or unavoidable maintainence.)

In this case there are two options:

#### (a) Continued Refresh

Calling the `ContinueDataRefresh` will, if there is exactly one in-progress data refresh, continue execution from the first unfinished stage. This is the recommended option for ensuring the refresh completes. 
 
#### (b) Manual cleanup
 
If you prefer not to continue a refresh, teardown must be performed.
This can either be done entirely manually, or the `RunDataRefreshCleanup` function can be run, which performs the described steps.
 
If this happens locally, you can likely ignore the infrastructure part of this checklist. (We do not expect this to ever occur in a deployed environment)

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


## MACs

### New MAC is showing as unrecognised

MACs are imported nightly - if a search is run for a brand new MAC less than a day after it was published, the MAC store will not be up to date, and a lookup error will occur. 

If MAC lookup fails:

- If the MAC is not valid
   
The exception is expected.

- If the MAC is valid, and <24 hours old

The MAC has not yet been imported. Manually trigger an early MAC import, or wait 24 hours.

If this happens frequently, consider increasing the frequency of the MAC import.

- If the MAC is valid, and >24 hours old

Check the MAC store in Azure storage, to see if it has been imported. 

If not, the import may be failing - check the Atlas Functions app logs in Azure Portal / Application Insights


