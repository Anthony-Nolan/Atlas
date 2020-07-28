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
      - Match Prediction database for global haplotype frequencies,
      - Search orchestrator function, to run search requests
  - Therefore, the remote environment should have been set up before running the framework.
  - User secrets are used to override the default local settings with the remote settings.
  - Open the `secrets.json` file.
    - In VS this is achieved by: Solution Explorer > `Atlas.MatchPrediction.Test.Verification` > Context Menu > "Manage User Secrets"
  - Add the following json with the appropriate values:
      ```json
      {
          "ConnectionStrings": {
            "MatchPrediction:Sql": "value"
          },
          TODO: ATLAS-477: extend with further settings
      }
      ```

### Generating a Test Harness
- This is achieved by running the API, as described above in the Start up guide, and calling the endpoint: `POST /test-harness`.
- All generated data is stored in the database defined within the app setting: `MatchPredictionVerification:Sql` (default: local db, `AtlasMatchPredictionVerification`):
- Generation involves the following steps:
  - Creating a new test harness entry - the ID of this is returned in the API result
  - Downloading the *currently active, global* haplotype frequency set from the database defined in app setting: `MatchPrediction:Sql`
    - Default is local, but should be overridden in user secrets, so as to point at the same database against which match prediction will be run.
  - Simulating patient and donor genotypes from a normalised pool of these haplotypes, and storing the simulants in the verification db.
  - Masking the simulated genotypes, and storing these new phenotypes in the db (each phenotype is linked back to its "parent", underlying genotype).

### Running Search Requests using Test Harness
TODO: ATLAS-477