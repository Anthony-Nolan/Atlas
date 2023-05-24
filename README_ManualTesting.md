# Manual Testing
Projects dedicated to manual, non-automated testing of various aspects of the Atlas solution.

> Important! This code is designed to be run locally; it is not production quality and cannot be deployed "as-is" to remote environments.

## Projects
- `Atlas.ManualTesting`
  - Functions project for the manual testing and debugging of Atlas.
  - e.g., Peek search-related service bus messages for info on search requests and results.

- `Atlas.MatchingAlgorithm.Performance`
  - A rudimentary harness for collating performance data of search times.
  - Relies on locally-run MatchingAlgorithm API.

- Match Prediction **Validation**
  - `Atlas.MatchPrediction.Test.Validation`
    - Functions project for the validation of the Match Prediction Algorithm (MPA).
    - Validation involves running match prediction requests for an externally-generated set of patients and donors, and compiling the match probabilty results for subsequent analysis.

  - `Atlas.MatchPrediction.Test.Validation.Data`
    - Project to manage Entity Framework migrations for the local Validation database.

- Match Prediction **Verification**
  - `Atlas.MatchPrediction.Test.Verification`
    - Functions project for the verification of the MPA.
    - Verification involves generating a harness of simulated patients and donors, running it through search requests, and collating the final results to determine the accuracy of match probabilities.

  - `Atlas.MatchPrediction.Test.Verification.Test`
    - Minimal suite of unit tests for the `Atlas.MatchPrediction.Test.Verification` project, with sufficient coverage only to ensure the reliability of the generated data.

  - `Atlas.MatchPrediction.Test.Verification.Data`
    - Project to manage Entity Framework migrations for the local Verification database.

## Mismatch count validation using exercises 1 and 2 from the WMDA consensus datasets
- The [WMDA consensus dataset exercises 1 and 2](https://pubmed.ncbi.nlm.nih.gov/27219013/) involves:
  - calculating total and antigen-level mismatches for every possible pair of patients and donors within the provided files;
  - generating an output file of results for comparison to the consensus results files provided;
  - analysis of any discrepancies.

### Generating mismatch results files
- The Atlas `ScoreBatch` function (under `MatchingAlgorithm.Functions`) is used to generate the required mismatch data:
  - Locally run the functions, `ProcessWmdaConsensusDataset_Exercise1` and `_Exercise2`, both within the `Atlas.ManualTesting.Functions.WmdaConsensusDatasetFunctions` namespace.
  - Local setting `Scoring:ScoreBatchRequestUrl` should be overridden with the `ScoreBatch` function URL for the instance of Atlas under test.
  - The patient and donor input files will first need to be modified to allow import:
    - The variety of field delimiters should all be replaced with a single character: `;`.
    - A header row should be added: `ID;A_1;A_2;B_1;B_2;DRB1_1;DRB1_2`
    - Locus names should be removed from the HLA typings (locus will be inferred by column name instead).

### Report and explanation of discrepant mismatch counts

##### Preparation
- Local setting `HlaMetadataDictionary:ConvertHlaRequestUrl` should be overridden with the `ConvertHla` function URL (under `MatchingAlgorithm.Functions`) for the instance of Atlas under test.
- The following header lines will need to be manually added to both the WMDA consensus text files and the Atlas results text files before import:
  - Exercise 1: `PatientId;DonorId;MismatchCountAtA;MismatchCountAtB;MismatchCountAtDrb1`
  - Exercise 2: `PatientId;DonorId;MismatchCountAtA;AntigenMismatchCountAtA;MismatchCountAtB;AntigenMismatchCountAtB;MismatchCountAtDrb1;AntigenMismatchCountAtDrb1`
- Ensure that both Atlas results files contain 10,000,000 lines each (excluding the header line).
  - If the count is lower, this is most likely due to scoring request failures.
  - Check the results file directory for a file named `failedScoringRequests.txt`, which will list the {Patient ID: Donor ID batch} for which scoring failed.
  - The `ProcessWmdaConsensusDataset_ExerciseX` functions can be re-run, with a "startFromPatientId" and "startFromDonorId" in the request body, to skip over Patient-Donor pairs that have already been processed.
  - Terminate the function once the missing information has been generated, and manually paste in the required rows into the main results file at the correct points (files are ordered by PatientId, then DonorId).
  - Alternatively, create new patient and donor files with only those subjects for which information is missing to serve as inputs.

##### Analysis of discrepant allele-level (a.k.a. "total") mismatch counts
- This should be run for both exercise 1 and 2 results files.
- Locally run the function, `ReportDiscrepantResults_TotalMismatches` within the `Atlas.ManualTesting.Functions.WmdaConsensusDatasetFunctions` namespace.
- The output file listing all the discrepant results (and the **PGroup** mappings that explain them) will be written to the same directory as the input Atlas results file, and will be named: `<Atlas-results-file-name>-total-discrepancies.txt`.

##### Analysis of discrepant antigen-level mismatch counts
- This can only be run for the exercise 2 results file.
- Locally run the function, `ReportDiscrepantResults_AntigenMismatches` within the `Atlas.ManualTesting.Functions.WmdaConsensusDatasetFunctions` namespace.
- The output file listing all the discrepant results (and the **serology** mappings that explain them) will be written to the same directory as the input Atlas results file, and will be named: `<Atlas-results-file-name>-antigen-discrepancies.txt`.

### Important considerations for data analysis
- In the event of discrepant results, consult the "scoringLog" files that were written out during the processing step that capture some of the original scoring results.
  - The `ScoreBatch` function can also be called directly with the patient-donor pair of interest to see the full scoring result.
- The exercise datasets include ARD and rel-dna-ser definitions, but using these would require setting up a new HLA nomenclature source and re-building the HLA Metadata Dictionary (HMD) using that source URL.
  - It is easier to use the latest HLA nomenclature version published by IMGT/HLA, and explain any discrepancies caused by using different HLA reference material.
  - This is a bigger issue for exercise 2 which involves antigen mismatch counting.
- Patient and donor HLA typings were encoded to HLA nomenclature version 2.16, which is too early a version for HMD creation.
  - As HMD lookup logic takes into account allele name changes over time, it should be ok to use a HMD created from the latest HLA nomenclature version available.
  - However, the presence of renamed/deleted alleles in the datasets may cause some discrepant results.
- Exercise 2 processer uses the `IsAntigenMatch` property on the positional scoring result to count antigen mismatches.
  - A count of 1 is assigned if the property is either `false` or `null`.
  - Value of `null` is assigned when either the patient or donor typing has no assigned serologies in referenced IMGT/HLA `rel_dna_ser` file, and so antigen match could not be determined.
- The exercise 2 reference file uses a letter-based code where a consensus on mismatch count could not be reached.
  - The Atlas mismatch count was deemed discrepant if it did not map to one of the mismatch counts represented by the letter.
  - E.g., if the reference count was `A` which represents `0` or `1` mismatches, but the Atlas count was `2`, then this would be reported as a discrepancy.

## Match Prediction Validation using data generated by a third party
Validation of the match prediction algorithm against a defined standard, such as [exercise 3 of the WMDA consensus dataset](https://pubmed.ncbi.nlm.nih.gov/27219013/).

### Dataset files
- Patient and donor data will be imported from their own text files with the following schema:
`ID;A_1;A_2;C_1;C_2;B_1;B_2;DRB1_1;DRB1_2;DQB1_1;DQB1_2`
  - This header line must be included, though the order of columns is not critical as long as the header is present.
  - Subjects not typed at the required loci (A, B, DRB1) will be ignored; debug output displays how many subjects were actually imported.
- Haplotype Frequency (HF) file should be uploaded to the target Atlas installation as the global HF set, prior to submitting match prediction requests.
  - Before upload, ensure that the HLA metadata dictionary on the target Atlas instance has the HLA version that the frequency file was encoded to.
  - The folder `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionValidation` contains an example of a script that converts a delimited text file to the required JSON file; it includes a step to "upgrade" v3.4.0 HLA values to v3.33.0 (the earliest HLA nomenclature version that Atlas supports).

### Stored Data
- At present, only data generated from the last validation run will be persisted to the associated Validation database (defined within the Functions app setting: `MatchPredictionValidation:Sql`)
  - Running a new import clears tables of all existing data.
  - Submitting match requests deletes existing requests and results, but patient and donor data is left intact.

### Running Validation
Validation functions and services are designed to be run locally, although they may be pointed to remote resources, e.g., you may wish to submit match prediction prediction requests to a deployed Atlas instance to leverage Azure app scaling.

#### One time Set-up
- Set up the Validation database by running EF core database migrations on `Atlas.MatchPrediction.Test.Validation.Data`.
  - Default connection strings in the `.Data` project app.settings and Function project settings both point locally. 
- Configure connection strings for other resources in the settings file of the `Atlas.MatchPrediction.Test.Validation` project (see below for specific guidance on what should be overridden).

#### Functions app
- The steps of validation are managed by individual functions within the `Atlas.MatchPrediction.Test.Validation` Functions app project.
- After starting up the Functions app (ideally **not** in debug mode for best perfomance):

1. Invoke the `ImportSubjects` function, submitting the locations of the patient and donor text files.
2. Send match predictions requests, either by invoking the `SendMatchPredictionRequests` function or `ResumeMatchPredictionRequests` function.
  - Before starting, set the request URL in the functions settings & manually create a new subscription to the `match-prediction-results` topic on the service bus.
  - At present, one request is sent per patient, with a subset of donors included as a batched request, to reduce the total number of http calls. Batch size is configurable via function settings.
  - Use the `Send...` function if you are running validation for the first time, or want to start a new run (**WARNING: all previous results will be wiped from the database**).
  - Alternatively, use `Resume...` function if you wish to resume sending requests from the last recorded patient with a match request, onwards; this is useful if the last invocation was interrupted before completion.
  - Note: the maximum size of the topic subscription is 5Gb, and it is possible to hit that limit if you are submitting several million requests. Once the limit is reached, the match prediction request endpoint will return 4xx responses until the algorithm has consumed some of the requests. It is worth keeping an eye on the topic size, and stop/resume requests sending accordingly.
3. The service-bus-triggered function, `ProcessResults`, will automatically download and store available results to the validation database.
  - Make sure to set the Azure storage and messaging bus connection strings, as well as subscription name, in the function settings.
  - Messages that contain algorithm request IDs not in the requests table will be ignored but will also be taken off the queue.
4. [Optional] In case any match prediction results were not downloaded, and their service bus messages have been taken off the queue, invoke function `PromptDownloadOfMissingResults`; this will query the database for those requests that are missing results, and send off new messages to the `results` topic, thereby kicking off the above described results-download process. This requires that the results files are still on the blob storage container.

## Match Prediction Verification using simulated data
Verification is the validation of match prediction using patient and donor data that has been simulated from a given HLA haplotype frequency dataset.
The exercise involves:
  - generating the patient and donor data, 
  - preparing the Atlas environment with all the required data,
  - running searches with match prediction enabled,
  - and, finally, analysing the results.

- [Glossary](#glossary)
- [Start Up Guide](#start-up-guide)
- [Test Harness Generation](#test-harness-generation)
  * [Pre-Generation Steps](#pre-generation-steps)
    + [Populate/refresh the Local MAC store](#populate-refresh-the-local-mac-store)
    + [Upload Haplotype Frequency Set File](#upload-haplotype-frequency-set-file)
  * [Generating the Test Harness](#generating-the-test-harness)
- [Prepare Atlas to receive Search Requests](#prepare-atlas-to-receive-search-requests)
- [Searching](#searching)
  * [Small g groups](#small-g-groups)
  * [Send Search Requests](#send-search-requests)
  * [Retrieve Search Results](#retrieve-search-results)
- [Verification Results](#verification-results)

### Glossary
- Test Harness: a set of simulated patient and donors that will be run through search in order to verify match prediction.
- Simulant: a simulated individual - either a patient or a donor - and their HLA typing (genotype and phenotype).
- Normalised Haplotype Pool: data source for genotype simulation; produced by assigning each haplotype within a set a copy number, based on its relative frequency.
- Masking: the process of converting a high-resolution genotype into a lower resolution phenotype; this may include deleting selected locus typings entirely.
- PDP: patient-donor pair.

### Start Up Guide
- Run Migrations on the Verification database
  - All data generated for purpose of verification will be stored in the database defined within the Functions app setting: `MatchPredictionVerification:Sql`.
    - The target of EF Core Migrations is set by a second connection string, `Sql`, in the `appsettings.json` of the `.Data` project.
    - By default, both connection strings point locally to db: `AtlasMatchPredictionVerification`.
    - The connection strings could be changed to point at a remote db, e.g., if you wish to share or archive the data.
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
          "Matching:ResultsTopicSubscription": "subscription-name",

          "MessagingServiceBus:ConnectionString": "remote-connection-string",
          
          "Search:RequestUrl": "url-to-search-function",
          "Search:ResultsTopicSubscription": "subscription-name"
        }
        "ConnectionStrings": {
          "DonorImport:Sql": "remote-connection-string",
          "Matching:PersistentSql": "remote-connection-string",
          "MatchPrediction:Sql": "remote-connection-string"
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
Note: at present, the framework is hard-coded to only run five locus (A,B,C,DQB1,DRB1) search requests.

#### Small g groups
- Atlas does not _officially_ support patients or donors typed with small g groups within end-to-end search.
  - However, to allow the calculation of matching PDPs for verification, minimal small g support has been added to various components.
- If the source HF set is encoded to small g typing resolution, then the masking proportions of the Test Harness should be 
  manipulated in such a way to ensure no small g groups remain in the masked patient and donor typings. 
- The verification framework automatically sends the simulated patient and donor genotypes to Atlas search
  as matching-only requests, i.e., search requests with scoring and MPA disabled.
- As MPA is not run on such genotypes, the "positive control" query included in
  `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification\VerificationResultsSQLQueries.sql` will not work.

#### Send Search Requests
- The last part of verification data generation involves sending search requests to the test environment
  for each test patient within the specified test harness.
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
- As search is an async process, service-bus triggered functions, `FetchMatchingResults` and `FetchSearchResults`, are used to retrieve
  the results when they are ready to download from blob storage.
  - Ensure `Matching:ResultsTopicSubscription` and `Search:ResultsTopicSubscription` has been overriden in `local.settings.json`
    with the remote environment value.
  - The property `serviceBus:"messageHandlerOptions:maxConcurrentCalls` determines how many results are processed at a time.
    - This can be changed to a count that is optimal for the local environment.
    - Note: the first batch of messages will take the longest to run, due to the loading of in-memory caches.
- The processing of some search results requires the scoring of genotype PDPs.
  - In addition to previously mentioned config parameters, also ensure that `Matching:PersistentSql` has been overriden
    in `local.settings.json` with the remote environment value.
- The functions app must be running for the function to listen for new messages.
  - As it may take several hours for all requests to complete, it is advised to either leave the app running in the
    background, or only launch it when it seems the majority of requests have completed.
  - The queries in `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification\VerificationRunSQLQueries.sql`
    help to determine overall results download progress.
- Make sure to check search servicebus topics/queues for dead-lettered messages, both requests and results-related.
  - Dead-letters are safe to replay; downloaded search results will overwrite any existing results mapped to the same request.
  - Check AI logs/Debug window for further info if messages repeatedly dead-letter.

### Verification Results
Verification results are displayed using an Actual vs. Potential (AvP) plot.

Recommended reading for how AvE plots and their metrics should be used and intepreted:
Madbouly, A., at al, (2014); Validation of statistical imputation of allele-level multilocus phased genotypes from 
ambiguous HLA assignments; Tissue Antigens, 84(3):285-92.

To generate an AvE plot:
1. Launch the verification functions app, and invoke the http-triggered function, `WriteVerificationResultsToFile`; it requires:
  - Verification run ID;
  - Directory where CSV files should be written out to.
    - Filenames will be auto-generated to contain the verification run ID and prediction type.
    - Any existing file in the specified directory with the same name will be overwritten without warning.
    - Reminder: any backslashes in the path should be escaped, i.e., `C:\\dir\\subdir`.
  - See Swagger UI for exact request model.
2. Open the generated AvE results file of interest, and copy all three columns.
3. Open the template file, `AvE_Plot_Template.xlsx`, located in `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification`.
  - Navigate to the tab, labelled: `ENTER DATA HERE`, and paste the copied data into the columns of the same name.
  - Save a copy of the file to the location of your choice.
4. Navigate to tab, `AvE Plot`, to see the AvE plot, as well as other metrics:
  - `n`: total number of PDP pairs
  - `WCBD`: Weighted City Block Distance
  - `Weighted R2`: R2 value calculated using the weighted mean of each match probability bin.

Verification metrics (without plots) for all predictions are also written out to a CSV file in the requested directory.

See also `\MiscTestingAndDebuggingResources\ManualTesting\MatchPredictionVerification\VerificationResultsSQLQueries.sql`
for additional queries that can be run after verification search results have been retrieved.