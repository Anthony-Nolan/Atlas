# Nova.SearchAlgorithm
Service for AN's HSC Search Algorithm.

## Projects

The solution is split across multiple projects: 

### Code Projects

- Nova.SearchAlgorithm
    - This project contains the actual business logic of the search algorithm 
- Nova.SearchAlgorithm.Api
    - An ASP.NET Core WebApi wrapper exposing functionality from the algorithm project
    - Note that this is *NOT* the entry point to the application in deployed environments. Due to restrictions 
    around long running requests when deployed in Azure, this api will not be exposed. 
    - Instead, the API is used for testing - it can be used locally for ease of development, and the performance and 
    validation test projects rely on the API
- Nova.SearchAlgorithm.Client
    - Exposes models needed to integrate with the search algorithm service from other services
- Nova.SearchAlgorithm.Common
    - Shared internal models / interfaces between the logic and data projects. 
- Nova.SearchAlgorithm.Data
    - Manages the transient database, i.e. pre-processed donor data. Entity Framework is used to manage the schema, but 
    *NOT* for querying the data
- Nova.SearchAlgorithm.Data.Persistent
    - Manages the persistent database, i.e. any data that does not require re-processing regularly. 
    - Uses Entity Framework for schema management and querying
- Nova.SearchAlgorithm.Functions
    - Azure Functions App - this is the main entry point for the algorithm, as many of its features are long running and 
    better suited to a functions app than a traditional web api. 
    - Note that this app should always be deployed to an *app service plan* not a consumption plan - as we require the 
    longer timeout of an app service plan, plus we do not want the app to automatically scale
- Nova.SearchAlgorithm.MatchingDictionary
    - Responsible for maintaining and accessing the "matching dictionary" - a set of tables in Azure Storage which act 
    as an interface for allele details published by WMDA

### Test Projects

- Nova.SearchAlgorithm.Test
- Nova.SearchAlgorithm.Test.Integration
- Nova.SearchAlgorithm.Test.Validation 
    - These projects are covered in detail in the testing section below. 
- Nova.SearchAlgorithm.Performance
    - A rudimentary harness for collating performance data of search times. 
    - Relies on hitting an API, so is only useful locally for now
        - To run on deployed environments we'll need to add auth to the API project and deploy to the relevant 
        environment    

## Start Up Guide

#### Development Environment

Some of the files in the matching dictionary tests are longer than the 260 character limit in Git for Windows. 
There are known issues trying to load the Test project in Visual Studio 2017 due to this. The issue is not present in 
Visual Studio 2019 (or Rider), so a compatible IDE will be necessary to load the test project.

#### Git Configuration On Windows

Some of the files in the matching dictionary tests are longer than the 260 character limit in Git for Windows. 
To get around this on a Windows machine, run the following command (as an administrator): 

`git config --system core.longpaths true`

#### Local Settings

Settings for each non-functions project are defined in the `appsettings.json` file. 

In some cases these settings will need overriding locally - either for secure values (e.g. api keys), or if you 
want to use a different service (e.g. donor service, azure storage account, service bus)

This is achieved with User Secrets: https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-2.2&tabs=windows

##### Azure Functions Settings

Azure functions requires a different settings configuration. With the "Values" object in `local.settings.json`, it expects a 
collection of string app settings - these reflect a 1:1 mapping with the app settings configured in Azure for deployed environments
    
> Warning! Attempting to use nested objects in this configuration file will prevent the app settings from loading, with no warning from the functions host

To enable a shared configuration pattern across both the functions project, and api used for testing, the Options pattern is used: 

- In the API project(s), we use the standard implementation of the [options pattern within .NET Core DI](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2), in `ServiceConfiguration.cs`
- In the Functions project, we bypass this configuration setup and instead explicitly register the IOptions<TSettings> objects we instantiate - in `Setup.cs`

#### Storage

The service uses two storage methods for different data, SQL and Azure Cloud Tables.

- Cloud Tables
    - The `StorageConnectionString` setting in `appsettings.json` determines the connection to Azure storage. 
    The default is to use local emulated storage, for which the *Azure Storage Emulator* will need to be running. 
    - To run against development storage (e.g. if performance of the emulator is not good enough, or the emulator is 
    unavailable), this connection string can be overridden using user secrets to point at the DEV storage account. (*DO NOT* check this is string in to git!)
    
- SQL
    - The service makes use of Entity Framework (EFCore) Code-First migrations. The models and repositories for data access
    are found within the `Nova.SearchAlgorithm.Data` and `Nova.SearchAlgorithm.Data.Persistent` projects.
    - Before running the app, migrations must be run using `dotnet ef database update -p <projectName>` from a terminal (or `Update-Database` from the nuget package manager)
        - Note that the data project maintains two databases, referred to as "A" and "B". EF core will use the app setting 
        for database "A" by default, defined in `ContextFactory.cs`. To locally test the hot-swapping feature, migrations will need to be
        run manually against both databases, A and B. In many cases just picking a database and always using one will be ok for 
        local development, as the swap will only occur when the hla refresh job is run. 
    - After changing any data models, a migration must be created with `dotnet ef migrations add -p <projectName>` (or `Add-Migration <migration-name>` in nuget package manager), then run as above
      - **Important Note Regarding Migrations:** The `MatchingHlaAt<Locus>` tables are so large that the entity framework 
      migration runner has been known to struggle to cope with large migrations of existing data. 
     In such cases the data may need to be manually migrated
     
     For the HLA refresh job to complete in a reasonable timeframe, indexes must be removed from the MatchingHlaAt<Locus> tables.
     Once the job is complete, they should be re-added. https://anthonynolan.atlassian.net/wiki/spaces/NPDWS/pages/541229066/Database+Setup contains a script to re-add the indexes, if the required indexes change then that page should be updated accordingly
     
#### Dependencies

The service has external dependencies on two services, the `DonorService` and `HlaService`. By default the configuration points to the 
deployed development instances of these service - locally the api keys for these services will need adding as user secrets. 

## Pre-Processing 

The service has three pre-processing stages that will need to be run locally before it will be possible to run a search.

Note that steps 2 and 3 are only independent when running a "full" donor import - i.e. importing all donors into a fresh database, then 
running an hla refresh. This full import will only happen when the algorithm is first deployed, and from then on every three months when the underlying
WMDA provided HLA information changes. The continuous donor import for new/updated donor information will import the donor and update HLA in the same step.

### (1) Matching Dictionary

The "Matching Dictionary" is a set of azure cloud storage tables containing nomenclature information about HLA.
The pre-processing job fetches up to date information from WMDA, and populates these tables with the information necessary to run a search

- Start the job by POST-ing to the `/matching-dictionary/recreate` endpoint
- The job is expected to take several minutes to run to completion
- The job will need re-running whenever 
  - (a) The schema is changed
  - (b) The data from WMDA is updated (every 3 months)

### (2) Donor Import

The donors against which we run searches are imported from Anthony Nolan's `Solar` Oracle database, via the `DonorService`.
We only store as much information as is needed for a search - ID, Registry, Donor Type, and HLA information.

- Start the job by POST-ing to the `/trigger-donor-import` endpoint
- The job is expected to take several hours to run
- The job will only be re-run in full when WMDA data is updated (every 3 months). 
    - A smaller donor import of only new/changed donors should be configured to run overnight (NOVA-2131. At time of writing, 07/08/2018, this is yet to be implemented)

### (3) Hla Refresh

For each donor, we expand all hla into corresponding p-groups, and store a relation in the appropriate `<MatchingHlaAt<Locus>` table

- Start the job by POST-ing to the `/trigger-donor-hla-update` endpoint
- The job is expected to take multiple hours to run
- The job will only be re-run in full when WMDA data is updated (every 3 months). 
    - New/changed donors should have these relations (re-)calculated as they change (NOVA-2131. At time of writing, 07/08/2018, this is yet to be implemented)

## Search

The primary purpose of the Search Algorithm is to run a search.
A search involves receiving some patient HLA, and returning all donors that are a potential match for that HLA.

A search request requires the following criteria:

- Donor type
    - Adult or Cord - only the specified donor type will be returned
- Registries to search
    - Only donors from specified registries will be returned
- Match Criteria
    - Allows specification of an allowed number of mismatches, both overall and per locus.
    - If a donor exceeds either the total mismatch count, or any per locus count, it will not be returned
    - Some loci (Dqb1, C) are optional - if omitted, matching will not be run against these loci
- Search Hla Data
    - The HLA data to search against. (Usually the hla of a patient - but occasionally searches will be run against modified HLA)
    -  All known HLA should be provided, even if that locus is omitted from matching 
        - This is because scoring will still be run against that locus, even if mismatches at it are not considered when matching donors
    
The search process can be broken down as follows:
        
### (1) Matching

The matching stage involves selecting which donors from the database to return from the search.

Matching is performed on a per locus level - each locus can be a 0/2, 1/2, or 2/2 match.
As each locus has two positions, matches can be in differing orientations. 

We refer to matches as 'cross' or 'direct': (*P = patient, D = donor*)

- Direct: P1 <=> D1, P2 <=> D2
- Cross: P1 <=> D2, P2 <=> D1


The worst match considered is a 'p-group' level match - i.e. the donor and patient alleles share a 'p-group'. Hence our matching strategy only considers p-groups, via the 'MatchingHlaAtX' tables set up in the pre-processing

The matching logic is split into three parts:

#### (a) Database level p-group matching

This involves running SQL queries against the MatchingHlaAtX tables, per locus.
Any donors with at least one match (shared p-group) at the given locus are returned.

- N.B. As untyped loci are considered potential matches, running this level of matching against the C and DQB1 tables (where many donors are untyped) will result 
in a large numebr of results. It is not recommended to match on these loci in the database for this reason.

These results will be filtered, such that only donors matching the mismatch criteria of all database-matched loci, plus the total mismatch criteria, are returned

#### (b) In-memory p-group matching

The loci that were not matched on in part (a) are now considered. We run an in memory comparison of the p-groups of all donors, removing any that do not fit the mismatch criteria at the specified locus, or where mismatches at the new locus cause the total mismatch count to be exceeded

This approach is significantly slower than database filtering for large numbers of donors. 
However, as our matching tables get quite large, it can be quicker than the database query when only performed on a small number of donors.

*Performance Optimisation*

Performance can be optimised by finding the correct balance between the two matching strategies. 

The current theory is that the best matching strategy is to run database matching on the smaller MatchingHlaAtX tables until a small enough set of donors are returned, at which point we switch to in memory matching.

However, extensive research into the best approach has yet to be performed, so the balance may need to be shifted to achieve optimal search performance.

#### (c) Further filtering

Finally, additional filtering is performed on the donors by e.g. registry, donor type.

This is performed last as fetching all donor information for matches should be avoided as long as possible, as until this point we have only had need of donors p-group information

### (2) Scoring

Once all matches have been retrieved, we must score them. 

This involves:

#### (a) Grading 

Each locus/position will be assigned a match grade, which will indicate the quality of the match.


|Typing Methods|Match Grade|Description|
|--------------|-----------|-----------|
|Both types are molecular|gDNA|Same nucleotide sequence across entire gene.|
|Both types are molecular|cDNA|Same nucleotide sequence across coding regions only.|
|Both types are molecular|Protein|Same polypeptide sequence across coding regions only.|
|Both types are molecular|G group|Same nucleotide sequence across ABD only.|
|Both types are molecular|P group|Same polypeptide sequence across ABD only.|
|---|---|---|
|One or both types are serology|Associated|Corresponding antigens are matched at associated level.|
|One or both types are serology|Split|Corresponding antigens are matched at split level.|
|One or both types are serology|Broad|Corresponding antigens are matched at broad level.|
|---|---|---|
|Any|Mismatch|Alleles do not match|

#### (b) Confidence

Each locus/position will be assigned a confidence, which indicates how likely the match is 

|Confidence Level|Resolutions|
|----------------|-----------|
|Definite|Both typings are molecular, and single allele resolution.|
|Exact|Both typings are molecular and map to a single P group.|
|Potential|Applies to all other pairs of matching types not described above, of any typing resolution.|
|Mismatch|The two types are mismatched; they can be of any typing resolution.|

#### (c) Ranking

The results will be ordered by a number of factors, including:

- Total mismatch count
- Weighted match grades
- Weighted match confidences

(The weightings will be defined in external storage, so they can be easily tweaked without a re-deploy)

N.B. The search team may want to account for other factors when viewing results, such as donor age/ethnicity.
This is currently not planned to occur in this service, but could be added in future.

## Testing

The solution has three levels of testing: Unit, Integration, Validation

### Unit Testing

Contained within the `Nova.SearchAlgorithm.Test` project.

No external dependencies or storage, testing individual code units. 

**Matching Dictionary Tests**

These tests use checked in versions of the allele data we fetch from WMDA. Originally they were directly copied from a version of the WMDA 
data, but to speed up testing, alleles unused in any unit test have been removed from some of the files - as such they should not be considered
to be valid representations of the WMDA data.

Any new alleles required in testing the matching dictionary should be added, and any no longer used can be removed.

### Integration Testing

Contained within the `Nova.SearchAlgorithm.Test.Integration` project.

- Uses a real SQL database, which is populated/cleared in each test run.
- External dependencies, and Matching Dictionary are stubbed out.
- Azure Storage emulator will need to be running - the tests should start this if it's not currently running, but it must be installed.
- Uses an independent DI setup, defined in `ServiceModule`. Uses publicly exposed helper methods from the core SearchAlgorithm project to ensure new dependencies only need registering once

These tests are especially useful for matching, where some logic is contained within the database layer and not covered in unit tests.

### Validation Testing

Contained within the `Nova.SearchAlgorithm.Test.Validation` project.

These tests are primarily for the benefit of non-developers, intended to confirm that the algorithm conforms to the specification
 to the Search Team's satisfaction.
 
- Uses a real SQL database, which is populated/cleared in each test run.
- Dependencies are not stubbed out (may change in future)
- Uses development azure storage account (may change in future)
- Starts an in-memory OWIN server, aiming to run the application as realistically as possible. 
  - All test implementations should be via HTTP requests to the in-memory service.
  - **SETUP:** As these tests spin up a full version of the application, local user secrets must be set up in the validation test project

- Tests are written in the Gherkin language, using the library `SpecFlow`
    - This allows the test suite to more more easily readable/reviewable/editable by non technical members of the Search and BioInformatics teams

### Secure Settings
The following keys must be set as user secrets in the api project:
- apiKey:{example-key}
- hlaservice.apikey
- donorservice.apikey

## Terminology

The following terms are assumed domain knowledge when used in the code:

* HLA
  - Human Leukocyte Antigen 
* WMDA
  - World Marrow Donor Association
* Homozygous locus
  - The typings at both positions for a locus are the same
  - e.g. *A\*01:01,01:01*
* Heterozygous locus
  - The typings at both positions for a locus are not the same
  - e.g. *A\*01:01,02:01*
* GvH = Graft vs. Host 
* HvG = Host vs Graft
  - Refers to the direction of matching if one party has a homozygous locus type
  - e.g. patient (host) is *A\*01:01,02:01*; donor (graft) is *A\*01:01,01:01* - There is one mismatch im the GvH direction (but none in the HvG)
  - e.g. patient (host) is *A\*01:01,01:01*; donor (graft) is *A\*01:01,02:01* - There is one mismatch in the HvG direction, (but none in the GvH)
* p-group
  - A set of alleles with the same antigen binding domain. See http://hla.alleles.org/alleles/p_groups.html
* g-group
  - A set of alleles with identical nucleotide sequences across the exons encoding the peptide binding domains. See http://hla.alleles.org/alleles/g_groups.html
