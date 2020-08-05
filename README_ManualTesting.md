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
      - Match Prediction database and azure storage for global haplotype frequencies,
      - Search orchestrator function, to run search requests
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
          TODO: ATLAS-477: extend with further settings
      }
      ```

### Generating a Test Harness

#### Upload Haplotype Frequency Set File
The first step is to upload the haplotype frequency (HF) set file that will be used by MPA for calculating match predictions,
as this must also be referenced for test harness generation.

- The file should be uploaded as the *global* set, to the remote environment where search requests will eventually be run, as described here:
  - TODO: ATLAS-627 - link to HF file upload README
- It is critical that the file be left in remote azure storage; *DO NOT* delete, modify, or overwrite the file once it has been uploaded.
- If the active, global HF set needs to be changed, upload a new file (ideally, with a unique name), and regenerate the test harness prior to running verification.

#### Generate Test Patients & Donors
- After the file has been successfully imported, launch the API, as described above in the Start up guide, and call the endpoint: `POST /test-harness`.
  - All data generated during this request will be stored in the database defined within the app setting: `MatchPredictionVerification:Sql`.
  - Default: local db, `AtlasMatchPredictionVerification`.

- Generation involves the following steps:
  1. Create a new test harness entry - the ID of this is returned in the API result.
  2. Retrieve the *currently active, global* HF set.
    - This step requires info stored in the remote MPA database.
      - The app setting, `MatchPrediction:Sql` must be overridden in user secrets to point at the remote MPA database.
    - Haplotype frequencies are read from the file that was used to generate the active, global HF set.
      - The app setting: `AzureStorage:ConnectionString`, must also be overridden in user secrets to point at the remote azure storage account.
  3. Simulate patient and donor genotypes from a normalised pool of these haplotypes, and store the simulants in the verification db.
  4. Mask the simulated genotypes, and store these new phenotypes in the verification db (each phenotype is linked back to its "parent", underlying genotype).

### Running Search Requests using Test Harness
TODO: ATLAS-477