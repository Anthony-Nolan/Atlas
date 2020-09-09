Projects dedicated to manual, non-automated testing of various aspects of the Atlas solution.
This code is designed to be run locally; it is not production quality and cannot be deployed "as-is" to remote environments.

## Projects
- `Atlas.MatchingAlgorithm.Performance`
  - A rudimentary harness for collating performance data of search times.
  - Relies on locally-run MatchingAlgorithm API.

- `Atlas.MatchPrediction.Test.Verification`
  - Functions project for the manual verification of the Match Prediction Algorithm (MPA).
  - Verification involves generating a harness of simulated patients and donors, running it through search requests,
	and collating the final results to determine the accuracy of match probabilities.

- `Atlas.MatchPrediction.Test.Verification.Test`
  - Minimal suite of unit tests for the `Atlas.MatchPrediction.Test.Verification` project, with sufficient coverage only to ensure the reliability of the generated data.

- `Atlas.MatchPrediction.Test.Verification.Data`
  - Project to manage Entity Framework migrations for the local Verification database.


## Verification

- [Glossary](#glossary)
- [Start Up Guide](#start-up-guide)
- [Test Harness Generation](#test-harness-generation)
  * [Pre-Generation Steps](#pre-generation-steps)
    + [Populate/refresh the Local MAC store](#populate-refresh-the-local-mac-store)
    + [Upload Haplotype Frequency Set File](#upload-haplotype-frequency-set-file)
  * [Generating the Test Harness](#generating-the-test-harness)
- [Prepare Atlas to receive Search Requests](#prepare-atlas-to-receive-search-requests)
- [Searching](#searching)
  * [Send Search Requests](#send-search-requests)
  * [Retrieve Search Results](#retrieve-search-results)

### Glossary
- Test Harness: a set of simulated patient and donors that will be run through search in order to verify match prediction.
- Simulant: a simulated individual - either a patient or a donor - and their HLA typing (genotype and phenotype).
- Normalised Haplotype Pool: data source for genotype simulation; produced by assigning each haplotype within a set a copy number, based on its relative frequency.
- Masking: the process of converting a high-resolution genotype into a lower resolution phenotype; this may include deleting selected locus typings entirely.

### Start Up Guide
- Run Migrations on the Verification database
  - All data generated for purpose of verification will be stored in the database defined within the app setting: `MatchPredictionVerification:Sql`.
    - By default, the connection string points to locally to db: `AtlasMatchPredictionVerification`.
    - This could be changed to point at a remote db, e.g., if you wish to share the data with other users, or archive the data.
  - Before verification can be performed, EF Core Migrations within the `Atlas.MatchPrediction.Test.Verification.Data` project must be run.
    - Instructions for VS PkgMgrCons:
      - Set Default Project (dropdown in PkgMgrConsole window) to be `Atlas.MatchPrediction.Test.Verification.Data`.
      - Set Startup Project (Context menu of Solution Explorer) to be `Atlas.MatchPrediction.Test.Verification.Data`.
      - Run `Update-Database` in the console.
      - Open your local SQL Server and verify the creation of the database: `AtlasMatchPredictionVerification`.
- Compile and Run the Functions app
  - Set Startup Project to `Atlas.MatchPrediction.Test.Verification`.
  - Compile and Run - a console window will pop-up, and a list of functions will be displayed if everything loads successfully.
  - Load the Swagger UI by copying the `SwaggerUi` function URL from the console into a web browser.
  - Scroll to the `HealthCheck` endpoint, fire it and confirm that you receive an `OK 200` response in under a second.
- Configure remote connection settings:
  - The verification framework runs locally, but depends on several deployed resources, namely:
      - Match Prediction database and azure storage for global haplotype frequencies;
      - HLA metadata dictionary (HMD) to generate masked typings - you could use the local storage emulator, but it is easier 
        (and perhaps faster) to use the remote HMD that has already been populated on the remote environment.
      - Donor import database and data refresh functionality to populate Atlas donor stores with test donors.
      - Search orchestrator function, to run search requests.
  - Therefore, the remote environment should be set up before running the framework locally.
    - Note: it is essential that the remote environment is dedicated to verification, so the framework can safely
      manipulate data as required and make assumptions about the state of various features.
  - Use the project's `local.settings.json` file to override default local values with the remote values:
      ```json
      {
        "Values":{
          "AzureStorage:ConnectionString": "remote-connection-string",
          ...
          "DataRefresh:RequestUrl": "url-to-force-data-refresh-function",
          "DataRefresh:CompletionTopicSubscription": "subscription-name",
          ...
          "HlaMetadataDictionary:AzureStorageConnectionString": "remote-connection-string"
          ...
          "MessagingServiceBus:ConnectionString": "remote-connection-string",
          
          "Search:RequestUrl": "url-to-search-function",
          "Search:ResultsTopicSubscription": "subscription-name"
        }
        "ConnectionStrings": {
          "MatchPrediction:Sql": "remote-connection-string",
          "DonorImport:Sql": "remote-connection-string"
        }
      }
      ```

### Test Harness Generation
- The test harness generator executes the following steps:
  1. Creates a new test harness entry with a unique ID.
  2. Retrieves the *currently active, global* HF set.
    - This step requires info stored in the remote MPA database.
      - The app setting, `MatchPrediction:Sql` must be overridden in user secrets to point at the remote MPA database.
    - Haplotype frequencies are read from the file that was used to generate the active, global HF set.
      - The app setting: `AzureStorage:ConnectionString`, must also be overridden in user secrets to point at the remote azure storage account.
  3. Simulates patient and donor genotypes from a normalised pool of these haplotypes, and store the simulants in the verification db.
  4. Masks the simulated genotypes, and stores these new phenotypes in the verification db (each phenotype is linked back to its "parent", underlying genotype).

#### Pre-Generation Steps
The following steps must be completed prior to generating the test harness.

##### Populate/refresh the Local MAC store
- For performance reasons, the masking step relies on expanded MAC definitions stored in the verification database, rather than the MAC dictionary.
  - This store must be populated before generating the first test harness.
  - This is achieved by calling the http-triggered function: `ExpandGenericMacs`.
  - This only needs to be done once, and will take a few mins to complete.
- It is advisable to refresh the local store (via the same function) before generating a new test harness for a later HLA nomenclature version,
    as new MACs are often created to cover changes to P and G groups.
  - Subsequent refreshes should complete faster, as only the latest codes are processed and stored.

##### Upload Haplotype Frequency Set File
- A haplotype frequency (HF) set file must be uploaded to the remote environment where search requests will eventually be run.
  - This file will serve as the data source when calculating MPA match predictions, and will also be referenced during test harness generation.
  - The file should be uploaded to the remote environment as the *global* set; [instructions in integration readme](README_Integration.md)
- It is critical that the file be left in remote azure storage; *DO NOT* delete, modify, or overwrite the file once it has been uploaded.
- If the active, global HF set needs to be changed, upload a new file (ideally, with a unique name), and regenerate the test harness prior to running verification.

#### Generating the Test Harness
- After the pre-generation steps have been successfully completed, launch the local functions app and call the http function: `GenerateTestHarness`.
  - Per locus masking instructions for patients and donors should be submitted within the request body (see Swagger UI for request model and masking options).
  - If successful, the unique ID of the generated test harness will be returned in the response.
  - Total time taken to complete the request will depend on the masking requests made (MACs take the longest to assign).
    - Requests tend to take on the order of minutes to complete.
    - Progress messages are written to the Debug output.

- Notes about masking:
    - Masking is applied on a per locus level; each locus is masked independently of the others.
    - Masking proportions should be submitted as rounded integer values, and their sum, per locus, should be between 0-100%, inclusive.
    - Within a locus, HLA typings are selected randomly for each masking category, according to the requested proportions.
    - `Delete` requests are not permitted at required matching loci (currently: HLA-A, -B and -DRB1).
    - A randomly selected typing will remain unmodified if it cannot be converted to the required masking category.
      - E.g., null-expressing alleles have no P group or serology.
      - E.g., several expressing alleles (especially at locus C) have no known serology assignment.
      - E.g., some alleles are not listed in the expanded definitions of any published MACs.
      - Etc.
      - This means that the proportions of masking categories found within the final test harness may differ to those listed in the request.

### Prepare Atlas to receive Search Requests
- Test donors need to be exported to the remote verification environment before search requests can be run.
  - Further, donor stores should only contain donors from one test harness at any one time.
- To achieve this, launch the local functions app and call the http function: `PrepareAtlasDonorStores`.
  - This takes in the ID of a completed test harness (see Swagger UI for request model).
  - The request will take several minutes to complete as it involves the following steps:
    1. Wipe the remote donor import donor store.
    2. Re-populate it with donors from the test harness specified in the request.
    3. Force a data refresh on the matching algorithm.
  - Ensure the following settings have been overriden in `local.settings.json` with values for the remote environment:
      `DataRefresh:RequestUrl`, `DataRefresh:CompletionTopicSubscription`, and `DonorImport:Sql`.
- As data refresh is a long-running process, a servicebus-triggered function, `HandleDataRefreshCompletion`, is
    used to determine when data refresh has actually completed.
  - The local verification functions app should be left running to allow the triggering of `HandleDataRefreshCompletion`.
  - On receipt of a new job completion message, the record for the last export attempt is updated with data refresh details,
    including whether the job succeeded or failed - a failure should prompt further investigation before re-attempting export.
- Make sure to check support alerts and AI logs for info on single donor processing failures.
- If the data refresh job is interrupted before it completes, manually invoke the `ContinueDataRefresh` function
  on the remote Matching Algorithm functions app.
- At present, it is not possible to tie an export attempt to a data refresh request *before* the refresh job has completed;
  some checks have been added to ensure only one export is attempted at a time.
  - If the `PrepareAtlasDonorStores` function complains about incomplete test donor export records, try the following:
    - Check whether a data refresh is still in progress; if so, leave the local functions app running so it can detect
      the job completion message and update the open export record.
    - If the data refresh job was interrupted, manually invoke the remote `ContinueDataRefresh` function. Again, leave
      the local verifications function app running to pick up the completion message.
    - If the interrupted data refresh job cannot be continued for some reason, then it is best to manually delete the open
      export record from the local verification database, and start from scratch.

### Searching
Note: at present, the framework is hard-coded to only run five locus (A,B,C,DQB1,DRB1) search requests,
  allowing up to 2 mismatches at any position in the donor's HLA. No scoring is requested.

#### Send Search Requests
- The last part of verification data generation involves sending search requests to the test environment;
  one for each test patient within the specified test harness.
- This involves launching the verification functions app, and invoking the http-triggered function, `SendVerificationSearchRequests`.
  - This takes in the ID of a completed test harness (see Swagger UI for request model).
  - It will return a verification run ID.
- The following conditions must be met for the request to be accepted:
  - Donors stores of the test environment should contain the donors of the specified test harness.
  - The HF set used to generate the specified test harness should be the active, global HF set on the test environment.
- If the required conditions are met, search requests will be sent via http and logged to the verification db.
  - Ensure `Search:RequestUrl` has been overriden in `local.settings.json` with the remote environment value.
  - Check the Debug window for progress.

#### Retrieve Search Results
- As search is an async process, a second service-bus triggered function, `FetchSearchResults`, is used to retrieve
  the results when they are ready to download from blob storage.
  - Ensure `Search:ResultsTopicSubscription` has been overriden in `local.settings.json` with the remote environment value.
- The functions app must be running for the function to listen for new messages.
  - As it may take several hours for all requests to complete, it is advised to either leave the app running in the
    background, or only launch it when it seems the majority of requests have completed.
  - The queries in `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification\VerificationRunSQLQueries.sql`
    help to determine overall results download progress.
- Make sure to check search servicebus topics/queues for dead-lettered messages, both requests and results-related.
  - Dead-letters are safe to replay; downloaded search results will overwrite any existing results mapped to the same request.
  - Check AI logs/Debug window for further info if messages repeatedly dead-letter.

### Verification Results
Verification results are displayed using an Actual vs. Potential (AvP) plot

Recommended reading for how AvE plots and their metrics should be used and intepreted:
Madbouly, A., at al, (2014); Validation of statistical imputation of allele-level multilocus phased genotypes from 
ambiguous HLA assignments; Tissue Antigens, 84(3):285-92.

To generate the AvE plot:
1. Launch the verification functions app, and invoke the http-triggered function, `WriteVerificationResultsToFile`; it requires:
  - Verification run ID;
  - Mismatch count, e.g., submit `1` if you want to verify P(1 mismatch);
    - Note: the framework does not currently write out individual locus predictions, only cross-loci predictions.
  - Directory where results CSV file should be written out to.
    - The filename will be auto-generated to contain the verification run ID and the mismatch count.
    - Any existing file in the specified directory with the same name will be overwritten without warning.
    - Reminder: any backslashes in the path should be escaped, i.e., `C:\\dir\\subdir`.
  - See Swagger UI for exact request model.
2. Open the generated results file, and copy the two columns labeled: `ActuallyMatchedPdpCount` and	`TotalPdpCount`.
3. Open the template file, `AvE_Plot_Template.xlsx`, located in `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification`.
  - Navigate to the tab, labelled: `ENTER DATA HERE`, and paste the copied data into the columns of the same name.
  - Save a copy of the file to the location of your choice.
4. Navigate to tab, `AvE Plot`, to see the AvE plot, as well as other metrics:
  - `n`: total number of PDP pairs
  - `WCBD`: Weighted City Block Distance
  - `Weighted R2`: R2 value calculated using the weighted mean of each match probability bin.
