# Summary
Service for the matching, non-predictive portion of the Atlas Search Algorithm.

## Table of Contents
- [Feature Documentation](#feature-documentation)
- [Technical Documentation](#technical-documentation)

## Feature Documentation

The primary purpose of the search algorithm is to run a MUD or CBU search.
A search involves receiving some patient HLA, and returning all donors that are a potential match for that HLA.

A search request requires the following criteria:

- Donor type
  - `Adult` (MUD) or `Cord` (CBU) - only the specified donor type will be returned
- Match Criteria
  - Allows specification of an allowed number of mismatches, both overall and per locus.
  - If a donor exceeds either the total mismatch count, or any per locus count, it will not be returned
  - Some loci (Dqb1, C) are optional - if omitted, matching will not be run against these loci
- Scoring Criteria
  - Allows specification of which loci should be scored, if any.
  - Allows specification of which loci should excluded from aggregation of score results, if any.
- Search Hla Data
  - The HLA data to search against (usually the HLA typing of the patient - but can be any set of valid HLA typings, such as "alternative phenotype", or a test phenotype as Atlas does not store any patient information)
  - All known HLA should be provided, even if that locus is omitted from matching, as it may be used by scoring, when enabled.

### (1) Matching

The matching stage involves selecting potentially matching donors from the database to return from the search.

Matching is performed on a per locus level - each locus can be a 0/2, 1/2, or 2/2 match.
As each locus has two positions, matches can be in differing orientations.

We refer to matches as 'cross' or 'direct': (*P = patient, D = donor*)

- Direct: P1 <=> D1, P2 <=> D2
- Cross: P1 <=> D2, P2 <=> D1

The worst match considered is a P-group level match ([deemed "AI3" in the WMDA matching framework](https://www.nature.com/articles/bmt2010132)) - i.e. the donor and patient typings share at least one P-group. Hence our matching strategy only considers P-groups for both molecular and serological typings.

### (2) Scoring

Once all matches have been retrieved, they can be scored.

This involves:

#### (a) Grading

Each locus/position will be assigned a match grade, which will indicate the quality of the match:

|Typing Methods|Match Grade|Description|
|--------------|-----------|-----------|
|**_Expressing vs. Expressing:_**|
|Both types are molecular|gDNA|Same nucleotide sequence across entire gene.|
|Both types are molecular|cDNA|Same nucleotide sequence across coding regions only.|
|Both types are molecular|Protein|Same polypeptide sequence across coding regions only.|
|Both types are molecular|G group|Same nucleotide sequence across ABD only.|
|Both types are molecular|P group|Same polypeptide sequence across ABD only.|
|One or both types are serology|Associated|Corresponding antigens are matched at associated level.|
|One or both types are serology|Split|Corresponding antigens are matched at split level.|
|One or both types are serology|Broad|Corresponding antigens are matched at broad level.|
|Any|Mismatch|Alleles do not match|
|**_Null vs. Null:_**|
|Both types are molecular|gDNA|Same nucleotide sequence across entire gene.|
|Both types are molecular|cDNA|Same nucleotide sequence across coding regions only.|
|Both types are molecular|Partial|Same name AND have partial g/cDNA|
|Both types are molecular|Mismatch|Mismatch at the molecular level but would be considered a match at the clinical level (i.e., assigned a match count of 1).|
|**_Expressing vs Null:_**|
|Any|Mismatch|Mismatch at the molecular level but may be matched at the clinical level in a homozygous scenario ([see below](#matching))|


#### (b) Confidence

Each locus/position will be assigned a confidence, which indicates how likely the match is:

|Confidence Level|Resolutions|
|----------------|-----------|
|Definite|Both typings are molecular, and single allele resolution.|
|Exact|Both typings are molecular and map to a single P group.|
|Potential|Applies to all other pairs of matching types not described above, of any typing resolution.|
|Mismatch|The two types are mismatched; they can be of any typing resolution.|

#### (c) Antigen-level matching

Each scored position will be assessed for whether it is an antigen match or not.
See [LocusPositionScoreDetails.IsAntigenMatch](/Atlas.Client.Models/Search/Results/Matching/PerLocus/LocusPositionScoreDetails.cs#ln29) for documentation of how this is calculated.

#### (d) Ranking

The results will be ordered by a number of factors, including:

- Total mismatch count
- Weighted match grades
- Weighted match confidences

(The weightings will be defined in external storage, so they can be easily tweaked without a re-deploy)

N.B. The search team may want to account for other factors when viewing results, such as donor age, CBU cell count, etc - presentation of the final match list is deemed the responsibility of the consumer that initiated the Atlas search, as it would hold the additional patient and donor data needed to make such front-end, user-facing decisions.

#### Miscellaneous

- Scoring of P- and G-group typings has been implemented and requires validation (TODO: #748).

### HLA Interpretation
As explained above, matching is performed at the P-group level and scoring requires additional knowledge of HLA typings (termed, "HLA metadata") to grade a match. This is achieved by looking up HLA typings in the **HLA Metadata Dictionary (HMD)**. Consult the [HMD README](/README_HlaMetadataDictionary.md) for information on [how HLA typings are interpreted](/README_HlaMetadataDictionary.md#hla-conversion) and how this can impact matching and scoring (especially when [comparing serology to molecular typings](/README_HlaMetadataDictionary.md#conversion-between-serology-and-molecular-typings)).

### Handling of Null Alleles
Null/non-expressing/"N" alleles are those HLA allotypes that do not result in a protein.

#### Matching
As matching is performed at the P-group level, if one of the two positions is an "N" allele, then only the P-group(s) of the expressing typing will be considered, i.e., the locus will be treated as homozygous for the expressing P-groups. 

E.g., `A*03:01,01:11N` will be treated as `A*03:01P,03:01P`, and would be assigned a match count of 2 when compared to another typing with the same pair of P-groups, such as `A*03:01,03:01`, `A*03:20,03:112`, `A*03:411,02:43N`, and so on.

#### Scoring
Null alleles have their own match grades ([see section on Grading](#a-grading)).
Note, the table shows where it is possible for a locus to have a match count of 1 or 2 due to the null-containing locus being treated as homozygous for the expressing P group, but still be assigned the score grade of `Mismatch` after the null allele is compared to another typing, one-to-one, at the molecular level.

#### MAC Interpretation
Null alleles present in the decoded/expanded allele string of a MAC are _not_ considered when matching or scoring. This behaviour was explicitly requested by the search coordinators and clinical scientist on the search algorithm development team as the expressed allele is more likely to be the true result (with some exceptions), and consideration of the null allele during search leads to undesirable results. Until the Transplant Centre updates the patient with a typing that specifies that there is a null allele, search coordinators work on the basis that the typing is expressed - and so Atlas ignores any null alleles in decoded MAC strings.

## Technical Documentation

### Projects

The solution is split across multiple projects:

#### Code Projects

- Atlas.MatchingAlgorithm
  - This project contains the actual business logic of the search algorithm
- Atlas.MatchingAlgorithm.Api
  - An ASP.NET Core WebApi wrapper exposing functionality from the algorithm project
  - Note that this is *NOT* the entry point to the application in deployed environments. Due to restrictions around long running requests when deployed in Azure, 
  this api will not be exposed.
  - Instead, the API is used for testing - it can be used locally for ease of development, and the performance and validation test projects rely on the API
- Atlas.MatchingAlgorithm.Client.Models
  - Exposes models needed to integrate with the search algorithm service from other services
- Atlas.MatchingAlgorithm.Common
  - Shared internal models / interfaces between the logic and data projects.
- Atlas.MatchingAlgorithm.Data
  - Manages the transient database, i.e. pre-processed donor data. Entity Framework is used to manage the schema, but *NOT* for querying the data
- Atlas.MatchingAlgorithm.Data.Persistent
  - Manages the persistent database, i.e. any data that does not require re-processing regularly.
  - Uses Entity Framework for schema management and querying
- Atlas.MatchingAlgorithm.Functions
  - Azure Functions App - this is the main entry point for the algorithm, as many of its features are long running and better suited to a functions app than a 
  traditional web api.
  - Note that this app should always be deployed to an *app service plan* not a consumption plan - as we require the longer timeout of an app service plan, 
  plus we do not want the app to automatically scale
- Atlas.MatchingAlgorithm.Functions.DonorManagement
  - Azure functions app responsible only for ongoing donor imports / updates
  - Needs to be an independent app so that the three-monthly full data refresh can disable these functions for the duration of the refresh

#### Test Projects

- Atlas.MatchingAlgorithm.Test
- Atlas.MatchingAlgorithm.Test.Integration
- Atlas.MatchingAlgorithm.Test.Validation
  - These projects are covered in detail in the testing section below.
- Atlas.MatchingAlgorithm.Performance
  - A rudimentary harness for collating performance data of search times.
  - Relies on hitting an API, so is only useful locally for now
    - To run on deployed environments we'll need to add auth to the API project and deploy to the relevant
        environment


### Storage

- Note that the matching data project maintains two databases, referred to as "A" and "B". EF core will use the app setting for database "A" by default, 
defined in `ContextFactory.cs`. To locally test the hot-swapping feature, migrations will need to be run manually against both databases, A and B. In many cases just picking
 a database and always using one will be ok for local development, as the swap will only occur when the data refresh job is run.

- **Important Note Regarding Migrations:** The `MatchingHlaAt<Locus>` tables are so large that the entity framework migration runner has been known to struggle to 
cope with large migrations of existing data.
  - In such cases the data may need to be manually migrated
      
### Pre-Processing

The service has three pre-processing stages that will need to be run locally before it will be possible to run a search.

Note that steps 2 and 3 are only independent when running a "full" data refresh - i.e. importing all donors into a fresh database, then processing hla. This full refresh will only happen when the algorithm is first deployed, and from then on every three months when the underlying WMDA-provided HLA information changes. The continuous donor import for new/updated donor information will import the donor and process HLA in the same step.

#### (1) HlaMetadata Dictionary

The "Hla Metadata Dictionary" is a set of azure cloud storage tables containing nomenclature information about HLA ([see HMD README](/README_HlaMetadataDictionary.md)).
The pre-processing job fetches up to date information from WMDA, and populates these tables with the information necessary to run a search.

- Start the job by POST-ing to the Triggering the `CreateLatestVersion` endpoint, in Swagger
- The job is expected to take several minutes to run to completion
- The job will need re-running whenever:
  - (a) The schema is changed
  - (b) The data from WMDA is updated (every 3 months)

#### (2) Donor Import

The donors against which we run searches are imported from the Atlas master data store, maintained in the "DonorImport" component.
We only store as much information as is needed for a search - ID, Donor Type, and HLA information.

- Start the job by triggering the `RunDonorImport` function
- The job is expected to take several hours to run
- The job will only be re-run in full when WMDA publish a new version of the HLA Nomenclature (every 3 months).

#### (3) Hla Processing

For each donor, we expand all hla into corresponding p-groups, and store a relation in the appropriate `<MatchingHlaAt<Locus>` table

- Start the job by triggering the `ProcessDonorHla` function
- The job is expected to take multiple hours to run
- The job will only be re-run in full when WMDA publish a new version of the HLA Nomenclature (every 3 months).

### Matching Implementation

Matching logic is split as follows:

#### (a) Database level p-group matching

This involves running SQL queries against the `<MatchingHlaAt<Locus>` tables.

These tables are queried in series, with the results from each locus providing a filter for the next locus, to minimise memory footprint and ensure 
the result dataset always shrinks.

(Note there is one exception - where all three required loci and permitted to have two mismatches, e.g. in a 4/8 or 8/10 search, no single locus can 
ensure all results are returned, so multiple loci must be queried unfiltered.)

> Performance Optimisation
>
> There is room for optimisation here, by ensuring that the queries used are the most efficient against the dataset. 
> It may be that some searches have different optimal queries (e.g. very ambiguous patients will have a lot of p-groups to query), and query selection could be made more dynamic 

#### (b) Further filtering

Finally, additional filtering is performed on the donors by e.g. donor type.

This is performed last as fetching all donor information for matches should be avoided as long as possible, as until this point we have only had need of donors p-group information

### Testing

The matching component has three levels of testing: Unit, Integration, Validation

#### Unit Testing

Contained within the `Atlas.MatchingAlgorithm.Test` project.

No external dependencies or storage, testing individual code units.

#### Integration Testing

Contained within the `Atlas.MatchingAlgorithm.Test.Integration` project.

- Uses a real SQL database, which is populated/cleared in each test run.
- External dependencies, and HlaMetadata Dictionary are stubbed out.
- Azure Storage emulator will need to be running - the tests should start this if it's not currently running, but it must be installed.
- Uses an independent DI setup, defined in `ServiceModule`. Uses publicly exposed helper methods from the core SearchAlgorithm project to ensure new dependencies only need registering once

These tests are especially useful for matching, where some logic is contained within the database layer and not covered in unit tests.

#### Validation Testing

Contained within the `Atlas.MatchingAlgorithm.Test.Validation` project.

These tests are primarily for the benefit of non-developers, intended to confirm that the algorithm conforms to the specification to the Search Team's satisfaction.

- Uses a real SQL database, which is populated/cleared in each test run.
- Dependencies are not stubbed out (may change in future)
- Uses development azure storage account (may change in future)
- Starts an in-memory OWIN server, aiming to run the application as realistically as possible.
  - All test implementations should be via HTTP requests to the in-memory service.
  - **SETUP:** As these tests spin up a full version of the application, local user secrets must be set up in the validation test project

- Tests are written in the Gherkin language, using the library `SpecFlow`
  - This allows the test suite to more more easily readable/reviewable/editable by non technical members of the Search and BioInformatics teams

See [The Validation Test README](Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/README_MatchingValidationTests.md) for a more detailed, non-technical overview

*Test Logging*

Some filesystem logging is configurable for the validation test suite, which will log the searched patient HLA and returned donor HLA, as well as 
match details.

This logging can be enabled for successful tests via the `app.config` file in the test project.

This can be particularly useful for debugging tests with auto-generated test data, to be sure of what searches are being run.

The logs are written to a sub-folder within the `bin` folder of the project.____  

*Test Data Errors*

- Note that there is a known, but infrequent, situation in which invalid test data can be selected.
    - TODO: ATLAS-465: Fix this invalid case

#### Secure Settings

The following keys must be set as user secrets in the api project:

- apiKey:{example-key}

*******