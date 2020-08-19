Projects dedicated to manual, non-automated testing of various aspects of the Atlas solution.
This code is designed to be run locally; it is not production quality and cannot be deployed "as-is" to remote environments.

## Projects
- `Atlas.MatchingAlgorithm.Performance`
  - A rudimentary harness for collating performance data of search times.
  - Relies on locally-run MatchingAlgorithm API.

- `Atlas.MatchPrediction.Test.Verification`
  - Web API project for manual verification of the Match Prediction Algorithm (MPA).
  - Verification involves generating a harness of simulated patients and donors, running it through search requests,
	and collating the final results to determine the accuracy of match probabilities.
  - Includes minimal suite of unit tests, with sufficient coverage only to ensure the reliability of the generated data.


## Verification

### Start Up Guide
- Run Migrations
  - Run EF Core Migrations for the `Atlas.MatchPrediction.Test.Verification` project.
  - Instructions for VS PkgMgrCons
    - Set Default Project (dropdown in PkgMgrConsole window) to be `Atlas.MatchPrediction.Test.Verification`.
    - Set Startup Project (Context menu of Solution Explorer) to be `Atlas.MatchPrediction.Test.Verification`.
    - Run `Update-Database` in the console.
    - Open your local SQL Server and verify the creation of the database: `AtlasMatchPredictionVerification`.
- Compile and Run API
  - Set Startup Project to `Atlas.MatchPrediction.Test.Verification`.
  - Compile and Run.
  - It should open a Swagger UI page automatically.
  - Scroll to the `api-check` endpoint, fire it and confirm that you receive an `OK 200` response in under a second.
- Configure remote connection settings:
  - The verification framework runs locally, but depends on some deployed resources to generate the test harness, namely:
      - Match Prediction database and azure storage for global haplotype frequencies;
      - HLA metadata dictionary (HMD) to generate masked typings - you could use the local storage emulator, but it is easier (and perhaps faster) to
        use the remote HMD that has already been populated on the remote environment.
      - Search orchestrator function, to run search requests.
  - Therefore, the remote environment should have been set up before running the framework.
  - User secrets are used to override the default local settings with the remote settings.
  - Open the `secrets.json` file.
    - In VS this is achieved by: Solution Explorer > `Atlas.MatchPrediction.Test.Verification` > Context Menu > "Manage User Secrets"
  - Add the following json with the appropriate values:
      ```json
      {
          "AzureStorage": {
            "ConnectionString": "value"
          },
          "ConnectionStrings": {
            "MatchPrediction:Sql": "value"
          },
          "HlaMetadataDictionary": {
            "AzureStorageConnectionString": "value"
          }
          TODO: ATLAS-477: extend with further settings
      }
      ```
### Glossary
- Test Harness: a set of simulated patient and donors that will be run through search in order to verify match prediction.
- Simulant: a simulated individual - either a patient or a donor - and their HLA typing (genotype and phenotype).
- Normalised Haplotype Pool: data source for genotype simulation; produced by assigning each haplotype within a set a copy number, based on its relative frequency.
- Masking: the process of converting a high-resolution genotype into a lower resolution phenotype; this may include deleting selected locus typings entirely.

### Test Harness Generation
- Note: All generated data will be stored in the verification database defined within the app setting: `MatchPredictionVerification:Sql`.
    - By default, the connection string points to locally to db: `AtlasMatchPredictionVerification`.

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
  - This is achieved by calling the endpoint: `POST /expanded-macs` on the Verification API.
  - This only needs to be done once, and will take a few mins to complete.
- It is advisable to refresh the local store (via the same endpoint) before generating a new test harness for a later HLA nomenclature version,
    as new MACs are often created to cover changes to P and G groups.
  - Subsequent refreshes should complete faster, as only the latest codes are processed and stored.

##### Upload Haplotype Frequency Set File
- A haplotype frequency (HF) set file must be uploaded to the remote environment where search requests will eventually be run.
  - This file will serve as the data source when calculating MPA match predictions, and will also be referenced during test harness generation.
  - The file should be uploaded to the remote environment as the *global* set; instructions here:
    - TODO: ATLAS-627 - link to HF file upload README
- It is critical that the file be left in remote azure storage; *DO NOT* delete, modify, or overwrite the file once it has been uploaded.
- If the active, global HF set needs to be changed, upload a new file (ideally, with a unique name), and regenerate the test harness prior to running verification.

#### Generating the Test Harness
- After the pre-generation steps have been successfully completed, launch the API and call the endpoint: `POST /test-harness`.
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

### Running Search Requests using Test Harness
TODO: ATLAS-477