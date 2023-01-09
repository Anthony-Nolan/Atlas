# Atlas Integration Guide

This README should cover the necessary steps to integrate ATLAS into another system, assuming it has been fully set up.


This document covers tuning the configuration of Atlas to suit your needs.

* PREVIOUS: [Deployment](./README_Deployment.md), [Configuration](./README_Configuration.md) 
* NEXT: [Support](./README_Support.md)

_____________________________

> On function invocation: All HTTP functions can be triggered via HTTP, and are authenticated via functions keys. These can be found and managed in Azure Portal.
> By default, two keys will exist per function - one named `master`, which has admin access to function endpoints, and one called `default` which just has functions
> access.
>
> For systems/developers, either the default key can be used, or specific ones can be generated manually.
> 
> To authenticate an HTTP request, set the appropriate function key as either a query param or header, as described in the [Azure documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp)
> (query param = `code`, header = `x-functions-key`)

## Notifications 

Several of the import process steps of ATLAS have some built in notification functionality. Two service bus topics will be created by terraform: 
`notifications` and `alerts`. Subscriptions can be added to these channels and consumed as appropriate for the installation. 

Azure logic apps can easily be set up to forward the notifications appropriately - e.g. to Slack, Microsoft Teams, etc...   


## Data Import

Before searches can be run, several sets of data must be imported into the system.

Some reference data is imported from third party services, such as NMDP and WMDA.
Other data must be provided by the owner of the Atlas installation - donor information, and haplotype frequency data.   



### Multiple Allele Codes

These will be automatically imported nightly, from source data [hosted by NMDP](https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip)

An import can also be manually triggered, if e.g. you don't want to wait until the next day to start using an ATLAS installation.

> The function `ManuallyImportMacs` should be called, within the `ATLAS-FUNCTIONS` functions app.



### HLA Metadata

HLA "metadata" refers to HLA naming conventions drawn from international HLA nomenclature, [hosted here](https://raw.githubusercontent.com/ANHIG/IMGTHLA/)

HLA metadata will be automatically imported for the latest stable version at the time of a matching algorithm data refresh (see below)

To enforce a recreation (e.g. in the case of a schema change), or to import an older version, manual endpoints can be hit.   

> The function `RefreshHlaMetadataDictionaryToSpecificVersion` should be called, within the `ATLAS-MATCHING-ALGORITHM-FUNCTIONS` functions app.



### Haplotype Frequency Sets

Haplotype frequency sets should be provided in the [specified JSON schema](Schemas/HFSetSchema.json).

They should be uploaded to the `haplotype-frequency-import` container within Azure Blob Storage. 
Anything uploaded here will be automatically processed, stored in the SQL database, and activated if all validation passes.

On success, a notification will be sent, and on failure, an alert.

Donors/patients will use an appropriate set based on the ethnicity/registry codes provided - such codes must be identical strings to those used in HF set import to be used.

A "global" set should be uploaded with `null` ethnicity and registry information - when no appropriate set is found for a patient/donor, this global default will be used.



### Donor Import

There are two supported donor import processes. Both use the [following JSON schema](Schemas/DonorUpdateFileSchema.json), and will be automatically 
triggered when any files are uploaded to the `donors` container in Azure Blob Storage.

In both cases, we expect performance to be better with several smaller files, than with larger ones.

> On Concurrency: The number of allowed concurrent processes of donor import files is controlled in two places. 
>
> (a) The host.json of the donor import functions app, which controls how many import processes can be run on one instance of the import. 
> (b) The application setting `WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT` (set via the terraform variable `DONOR_IMPORT_MAX_INSTANCES` at release-time), which controls
> how many instances the donor import can scale out to.
>
> We recommend keeping both values at 1, i.e. disallowing concurrent import processing. Allowing concurrency introduces a risk of transaction deadlocks when writing 
> updates to the database. 
>
> If set higher, a full import will likely finish a little quicker - but there will be some level of transient failures caused by deadlocks. Auto-retry logic should
> ensure that all donors are still imported, but any import files that hit the retry limit and are dead-lettered (on the `donor-import-file-uploads` service bus topic) will need to be replayed.      

#### Full Import

This process is maximally efficient, but does not propagate changes to the matching algorithm as standard - that will need to be manually triggered once all donor files 
have imported - see the "Data Refresh" section below.

This process is expected to be used as a one-off when installing ATLAS for the first time, to import all existing donors at the time of installation.

It should be able to handle millions of donors in a timeframe of hours. Performance can be improved further by manually scaling up the "-atlas" shared SQL database in Azure for
the duration of the import process.

Recommended donor file size is 10,000 - 200,000 donors per file.

#### Differential Import

This process is intended for ongoing incremental updates of donors - donors can be added, removed, or updated via this process. You can use "Upsert" change type - it will create the donor if it hasn't created yet or update the existing one. It could be handy if you don't track the state of the imported donors by the Atlas system. 

Differential donors will be automatically pre-processed for the matching algorithm, so there is no need to run a Data Refresh after triggering it.

It should be able to handle tens of thousands of donors in a timeframe of hours.

*NOTE*

Due to the use of EventGrid to trigger the import process, ordering of donor file processing cannot be guaranteed. There are code-measures in place to prevent donor *updates*
being applied out of order - but there are some edge cases that may still cause errors in very rare cases - e.g. if a donor is created and then updated in two separate files, 
which are uploaded within a very short timeframe of one another. Such edge cases are described in detail in [the donor import readme](README_DonorImport.md) - they are considered
extremely unlikely, and alerts will be sent if the data is ever detected as being out of sync. 

If ordering of donor updates is even 100% guaranteed, we recommend replacing the file-based ongoing donor import process with a purely service bus based one, and controlling 
the order of updates before they make it to ATLAS.  



### Data Refresh

The "data refresh" is a manually triggered job in the matching algorithm. 

> It can be triggered via the `SubmitDataRefreshRequestManual` function in the `ATLAS-MATCHING-ALGORITHM-FUNCTIONS` function app

The data refresh performs several operations: 

(a) Updates the Hla Metadata Dictionary to the latest version, if necessary. 
(b) Replaces all donors in the matching algorithm with the contents of the `Donor Import` component's donor database
(c) Performs pre-processing on all donors that is a pre-requisite to running searches 

The donor replacement is performed on a secondary matching algorithm database, which is only activated for search on completion - so running this process will not 
affect running searches. 

This process is expected to be run: 
- When first installing an ATLAS installation, after a "Full" donor import has finished
- Every three months, when new HLA nomenclature is published.

The process is expected to take several hours - and will increase with both the number of donors, and the ambiguity of their HLA. 
For a dataset with ~2 million donors, the process takes 1-2 hours.

 
## Running Searches
 
 Once all the data described above has been imported, Atlas will be ready to perform searches!
 
### Initiating a search
 
 > Search is triggered via a POST request to the `Search` function in the `ATLAS-PUBLIC-API-FUNCTION` function app

The search request should follow the format described [in this model](Atlas.Client.Models/Search/Requests/SearchRequest.cs) 

The properties of the search request are documented via XML doc-comments in this model, so will not be duplicated here.


This endpoint will: 
- (a) Validate the input of your search request, returning a 400 HTTP response code if the request is invalid
- (b) Trigger an asynchronous search process
- (c) Synchronously return a unique search identifier, which you should store, to cross reference search results to when they complete.


#### Search Request Validation

Validation rules are implemented in the following files: [Matching](Atlas.MatchingAlgorithm/Validators/SearchRequest/SearchRequestValidator.cs), 
[Match Prediction](Atlas.MatchPrediction/Validators/MatchProbabilityInputValidator.cs), [Repeat Search](Atlas.RepeatSearch/Validators/RepeatSearchRequestValidator.cs)

Slightly more human-readable documentation of the rules is as follows:

##### Initial Search 

- `DonorType` must be present, and an allowed value - 'Adult' or 'Cord'
- `SearchHlaData` must be present. If match criteria have been provided for a locus, then it must have non-null HLA values
  - All HLA provided must be recognised as valid HLA typings
    - If the HLA is not recognised, a failure notification will be sent asynchronously via the atlas repeat results topic
- `MatchCriteria`
  - A, B, DRB1 criteria must always be present
  - When present, per locus allowed mismatch count must be between 0-2 (inclusive) 
  - DPB1 may never be specified - as the algorithm is not capable of matching on this locus. Instead information from "scoring" should be used
  - Overall mismatch count may not be higher than 5. (Note that higher mismatch counts will lead to exponentially slower searches!)
- `ScoringCriteria`
  - List of loci to score must be provided - but this list may be empty!
  - List of loci to exclude from scoring aggregation must be provided - again, it may be empty!
 
##### Repeat Search

- `SearchRequest` must follow all validation rules described above
  - In addition, this is expected to be identical to the search request detail used for the initial version of the search. If any of this data (e.g. patient hla, match criteria) changes, then a brand new search should be run, 
  not a repeat search. If the provided data differs to the original search, behaviour of the algorithm is undefined
- `OriginalSearchId` must be provided, and must match a search request ID previously run through the initial search process
  - If the search request id is not recognised, a failure notification will be sent asynchronously via the atlas repeat results topic
- `SearchCutoffDate` must be provided

### Receiving search results

When a search is complete, a message will be sent to the `search-results-ready` subscription on the ATLAS service bus.

Consumers of ATLAS should set up a subscription for their application, and a listener for messages on said subscription.

The message contains the search ID for identification, some metadata about the search request - i.e. how many donors were matched, how long the search took - as well as a blob storage 
container and filename at which the results can be found. 

The results can then be downloaded from this blob storage container. They will be stored as JSON objects, [in the format described in this model](Atlas.Client.Models/Search/Results/SearchResultSet.cs)

**A consumer of ATLAS results notifications will need to be granted access to the following:**

- Read access to the appropriate service bus 
- Read access to the blob storage account

> IMPORTANT! 
>
> Consumers of the results messages must be idempotent. In some cases, multiple messages will be received for a single search request. 
> The most common case of this is a transient search failure - this will trigger a failure notification, then retry.
> This can lead to: 
> (a) A failure notification for a search request, followed by a success. (In the case of a truly transient error)
> (b) Several failure notifications in a row (In the case of an unexpected error that is present for all retries of a search - e.g. an Azure outage, or unexpected error with a specific search)


#### Receiving matching results early

As match prediction can take significantly longer to run than the matching algorithm, it is possible to retrieve results from just the matching algorithm (which includes "scoring" information, if requested) 
as soon as it's complete. 

This could be useful for e.g. showing users how many results are expected as soon as we know, so they are not surprised when a large result set takes a long time to fully complete.

These results follow the same pattern as the full search result sets. The service bus topic used is `matching-results-ready`, and the [following format is used](Atlas.Client.Models/Search/Results/ResultSet/ResultSet<MatchingAlgorithmResult>.cs) 


## Running results analysis only

ATLAS' primary purpose is to run as a full matching engine - i.e. matching donors for a search request, and analysing the results.

It is possible to run each of the results categorisation processes - "Scoring" (non-statistical) and "Match Prediction" (statistical, requires haplotype frequency sets) - independently,
on a per-donor basis.

These routes are not officially supported, so have not been rigorously load tested / documented - but the option is available.
