# Development "Zero-to-Hero" Start Up Guide

## Install IDEs/Tools
- *(All easily findable by Googling and appear to be generally happy with standard install settings.)*
- Install a compatible IDE: Visual Studio (2019 onwards) or Rider.
- Install and start Azure Storage Emulator (either `Azurite`, or the original, but now deprecated `Azure Storage Emulator`).
  - Note that, despite its appearance, the Emulator isn't really a service and does not auto-start on login by default.
    This can be achieved by making it a Startup app, e.g., by pasting a shortcut to the app into the Windows Start-up folder.
  - Mac users: There is no storage emulator on mac, so instead the storage connection string should be locally configured to use a deployed storage account.
- Install Azure Storage Explorer.
- Install SQL Server
- Install Postman (or some other means of easily running HTTP requests against the API)
- Install Service Bus Explorer

## Clone the repo with suitable git path settings
- Run `git -c core.longpaths=true clone git@github.com:Anthony-Nolan/Atlas.git`.
- Navigate into your newly cloned repo, and run `git config core.longpaths true`.

## Running Functions Locally
- Before running a functions app locally, make sure to amend the necessary app settings via the `local.settings.json` file.
  - The placeholder value: `override-this` is used to highlight those settings that must be overriden for the functions app to run successfully, e.g., service bus connection strings.
  - These files are ignored by git to prevent the accidental sharing of secrets.
  - Read the [Settings README](README_DevelopmentSettings.md) for more details about Atlas settings, and how to work with them when developing.

How to run functions via Swagger using Visual Studio:
- To run a function set it as your start-up project and run it.
- If Swagger has been configured for that Function app, the Swagger web addresses should be shown in the list of Http Functions.
- Simply copy this address into your browser and you will be able to trigger the functions from there.
- Trigger the HealthCheck endpoint in Swagger to make sure the function is working correctly.

## Service Bus Messaging
- There is no emulator for azure service bus, so a new bus will need to be manually set up in Azure for use when developing locally.

### Alerts & Notifications
- On the bus instance used for local dev, manually create two topics, `notifications` and `alerts`
  - These will be the destination for various support level messages.
  - Refer to [Support README](README_Support.md) for more info.
- For messages to be successfully sent out, `NotificationsServiceBus:ConnectionString` will need be overridden in the project being run.

## Distributed Cache (optional)
- The apps run without a distributed cache. When `DistributedCache:ConnectionString` is empty, every read of the cache misses.
- To use a local Redis, start one in a container: `docker run -d --name atlas-redis -p 6379:6379 redis:7.4` (Azure Managed Redis runs Redis 7.4).
  - Set `DistributedCache:ConnectionString` to `localhost:6379`: in `local.settings.json` for a Functions app, and in user secrets for the two workers.
  - Keep `DistributedCache:ManagedIdentityClientId` empty. It is for Azure Managed Redis, which accepts only Microsoft Entra ID sign-in.
- To remove the container: `docker rm -f atlas-redis`.

## Running A Search
Follow these steps to get to a state in which you can run Searches against a database of donors.
The details of why these steps are necessary and what they are doing, is detailed in the rest of the core README.
It's highly recommended that you read the sections outside ZtH in parallel with it. Especially if anything doesn't make sense!

### Run Migrations
- Run EF Core Migrations for all data projects:
    - `MatchingAlgorithm.Data`, `MatchingAlgorithm.Data.Persistent`, `DonorImport.Data`, `MatchPrediction.Data`, `RepeatSearch.Data`, `SearchTracking.Data`
    - This can be done from general command line, or from the VS Package Manager Console, but in either case **must be run from within those project folders!**.
- Instructions for the command line
  - From inside each project folder (e.g. `<gitRoot>/Atlas.MatchingAlgorithm.Data`), run `dotnet ef database update`.
    - Do not pass `-p <ProjectName>` here: it is resolved relative to the current folder, so it points at a folder that does not exist and fails with "Unable to retrieve project metadata".
- Instructions for VS PkgMgrCons
  - Open the Nuget Package Manager Console (Menus > Tools > Nuget Package Manager > ...)
  - Run `Update-Database -project <ProjectName> -startupProject <ProjectName>` in the console.
    - Note: if you have both EF 6 and EF Core commands installed, you may need to prefix the command with `EntityFrameworkCore\` to ensure the correct version is selected for execution.
  - This should take 10-40 seconds to complete.
- Having created all the local databases, run this additional step to set up the second transient Matching algorithm database:
    - From inside `<gitRoot>/Atlas.MatchingAlgorithm.Data`, run the migrations again with a connection string that points at `AtlasMatchingB`:
      `dotnet ef database update --connection "Data Source=(local);Initial Catalog=AtlasMatchingB;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"`
    - This avoids editing the checked-in `Atlas.MatchingAlgorithm.Data\appsettings.json`.
- On completion of migrations, open your local SQL Server and verify that you now have 3 databases: `Atlas`, `AtlasMatchingA` and `AtlasMatchingB`.

### Set up initial data

#### Import Donors
- In Storage Explorer, open your local emulator and create a new blob storage container called `donors`.
- Upload the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors.json` to your `donors` container.
    - This should take < 1 second to run.
- Open up Service Bus Explorer and connect to your local development Service Bus. (Make sure your local settings are set up accordingly)
- Create the topic `donor-import-file-uploads` with subscription `donor-import`, right-click the topic and select `Send Message`.
    - Copy the content of the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors-metadata.json` into the `Message Text` text box and click `Start`.
- When you now run `DonorImport.Functions` your local Donors table should now be populated.
    - This should take < 5 minute to run.
- Monitor the `donor-import-results` topic for import results; a failure will be reported to `alerts` topic as well.

#### Run an Initial Data Refresh
- Open up Service Bus Explorer and connect to your local development Service Bus.
- Create the topic, `data-refresh-requests` with subscription `data-refresh`.
- Create the topic, `completed-data-refresh-jobs`. It needs no subscription: the refresh publishes a completion message to it when the job ends.
- Create the topic, `donor-genotype-precomputation-requests` with subscription `precomputation-worker`. The donor genotype precomputation workers read the subscription, and `MatchingAlgorithm.Functions.DataRefresh` reads its dead-letter queue.
- Set up the donor genotype precomputation worker, `Atlas.MatchingAlgorithm.PrecomputeWorker`. It computes the work of data refresh stage 65 (see the [Matching Algorithm README](README_MatchingAlgorithm.md#donor-genotype-precomputation-data-refresh-stage-65)).
  - Its settings are in `appsettings.json`, with local defaults for SQL Server and Azurite.
  - Override `MessagingServiceBus:ConnectionString` and `ApplicationInsights:InstrumentationKey` with user secrets, as for the [Match Prediction worker](README_MatchPredictionAlgorithm.md#worker-project-configuration).
- Run `MatchingAlgorithm.Functions.DataRefresh` and in Swagger UI (or any other API development environment) trigger the `SubmitDataRefreshRequestManual` endpoint.
  - Set `forceDataRefresh` to `true`.
  - You should get a 200 Success response almost immediately but the refresh itself will take ~15 minute to run (do NOT close the function app down until it's complete!).
- Run `Atlas.MatchingAlgorithm.PrecomputeWorker` too. Keep it running until the refresh is complete.
  - You can start the worker before or after the trigger. It reads no database when it starts: for each message, it reads the data refresh record that the message names.
  - Stage 65 waits until the worker has processed every batch. If no worker runs, the refresh waits, and sends a stall alert after `DataRefresh:Precompute:StallAlertMinutes` (60 by default).
  - On the first refresh, there is no haplotype frequency set yet (you import one in the next step). So every batch fails, and the requeue timer sends each batch again `DataRefresh:Precompute:MaxBatchRetries` times, every 5 minutes. Set `MaxBatchRetries` to `0` in `local.settings.json` to make this faster.
  - Then all donors have failed. This is above `DataRefresh:Precompute:MaxFailedDonorFraction`, so the refresh pauses for a manual decision (see [the paused data refresh](README_Support.md#the-paused-data-refresh)). Send the `Complete` decision to the `ResolvePausedDataRefresh` endpoint, with the id of the data refresh record: `{ "DataRefreshRecordId": <id>, "Decision": "Complete" }`. The refresh then completes with no stored genotype sets.
  - To prevent the pause, set `MaxFailedDonorFraction` to `1` in `local.settings.json` before you trigger the refresh.
  - To store the donor genotype sets, run the data refresh again after you import the haplotype frequency set.
- Monitor the `notifications` topic to ensure the refresh succeeded; a failure will be reported via `alerts` topic.

#### Importing Haplotype Frequency Sets
- In Storage Explorer, open your local emulator and create a new blob storage container called `haplotype-frequency-set-import`
- Edit the `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/initial-hf-set.json` file so `nomenclatureVersion` is set to the nomenclature version that was used in the data refresh.
    - The nomenclature version that was used in the data refresh can be found in SQL Server - `Atlas` -> `MatchingAlgorithmPersistent.DataRefreshHistory` and then the `HlaNomenclatureVersion` for the active database.
- Upload the json file `initial-hf-set.json` to your `haplotype-frequency-set-import` container.
    - This should take < 1 second to run.
- Open up Service Bus Explorer and connect to your local development Service Bus.
- Create the topic, `haplotype-frequency-file-uploads` with subscription `haplotype-frequency-import`, right-click and select `Send Message`.
    - This is the topic set in `MessagingServiceBus:ImportFileTopic` in the `MatchPrediction.Functions` settings.
    - Copy the content of the json file `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/initial-hf-set-metadata.json` into the `Message Text` text box and click `Start`.
- When you now run `MatchPrediction.Functions` your local HaplotypeFrequencies and HaplotypeFrequencySets tables should now be populated.
    - This should take < 5 minute to run.
- Monitor the `notifications` topic to ensure the import succeeded; a failure will be reported via `alerts` topic.
- Note: It's possible that the `initial-hf-set.json` may fail to import due to the HLA therein being invalid according to the stated `nomenclatureVersion`.
    - In case of invalid HLA, you will need to manually correct the affected typings and repeat the import steps.

### Run a search (whilst avoiding MAC lookups)
- In Storage Explorer, open your local emulator and create two new blob storage containers called `atlas-search-results` and `matching-algorithm-results`.
- Open up Service Bus Explorer and connect to your local development Service Bus.
  - Create the topic, `matching-requests` with subscription `matching-algorithm`.
  - Create the topic, `matching-results-ready` with subscription `match-prediction-orchestration`.
- Run `Atlas.Functions.PublicApi`, `Atlas.Functions`, and `Atlas.MatchingAlgorithm.Functions` all in parallel.
  - To do this in VisualStudio first set your default project to `Atlas.Functions.PublicApi` and then run it.
  - Then in solution explorer right click on `Atlas.Functions`, and `Atlas.MatchingAlgorithm.Functions` and select `Debug -> Start New Instance`.
  - Alternatively you can right click on your solution in Solution Explorer go to properties and under multiple start up projects select all the functions.
  - *You will want to make sure all the local settings for these functions are up to date*
- Then hit the `Search` endpoint within `Atlas.Functions.PublicApi` with the content of `<gitRoot>\MiscTestingAndDebuggingResources\SearchRequests\initial-search.json` as the requests body.
  - You should get a 200 Success response.
- In the `atlas-search-results` blob storage you have created you should have a file containing the search results.
  - The first search should take 20-60 seconds.
  - Subsequent searches should take < 1 second.

## Matching Algorithm API
It can be useful to run the Matching Algorithm API when locally debugging and testing the matching algorithm component.
- Set Startup Project to `Atlas.MatchingAlgorithm.Api`.
- Compile and Run.
- It should open a Swagger UI page automatically. (Expected url: <https://localhost:44359/index.html>)
- Scroll to the `api-check` endpoint, fire it and confirm that you receive an `OK 200` response in under a second.
- Configure local keys and settings:
  - These values are stored as a local `user secret`, for the config setting, in the API project.
  - In VS this is achieved by:
    - Solution Explorer > `Atlas.MatchingAlgorithm.Api` > Context Menu > "Manage User Secrets"
    - This will open a mostly blank file called `secrets.json`.
    - Recreate the relevant portion of the nested structure of `appsettings.json` for the above config setting, inserting your new local settings where appropriate.
    - Note that this is a nested JSON object, so where we describe a `Foo.Bar` setting below, you'll need to create:

    ```json
    {
        "Foo": {
        "Bar": "value"
        }
    }
    ```

    - Set the following connection strings:
      - `MessagingServiceBus.ConnectionString`
      - `NotificationsServiceBus.ConnectionString`
      - `MacDictionary.AzureStorageConnectionString` (Optional)
        - It is recommended to point at an existing MAC dictionary on a remote dev/test system, in order to avoid having to populate your local emulator's MAC Dictionary (which takes several hours).

## Integration Tests
- The integration test suites that use a database create and migrate it themselves; they only need a SQL Server to connect to.
  - The matching and donor import migrations use `ONLINE` index operations, which need the Developer or Enterprise edition of SQL Server.
    LocalDB and the Express edition cannot run them.
- `Atlas.MultipleAlleleCodeDictionary.Test.Integration` requires a connection to an Azure Storage instance.
  By default, it will look for a local storage instance, which would require Azurite to be running.
- The connection strings default to a local SQL Server, in each test project's `appsettings.json`.
  They can be overridden with environment variables, e.g. `ConnectionStrings__SqlA`, as the system tests pipeline does.

## Validation Tests
The matching validation tests need the same SQL Server as the integration tests, and nothing else:
the HLA Metadata Dictionary and the MAC Dictionary are read from files, and the Service Bus clients are substituted.

## Running the Integration and Validation Tests in Docker
Instead of installing SQL Server and Azurite, you can run them in containers, as the system tests pipeline does.
This needs a container runtime, e.g., Docker Desktop, Rancher Desktop or Podman.

### Start the containers
These are the image versions the pipeline uses (see `build-pipeline-run-tests.yml`); keep them in step with it.
SQL Server is published on port 14330, so that it does not clash with a SQL Server installed locally on 1433.

```bash
docker run -d --name atlas-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Atlas-System-Tests-1 -p 14330:1433 mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04
docker run -d --name atlas-azurite -p 10000-10002:10000-10002 mcr.microsoft.com/azure-storage/azurite:3.37.0 azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck
```

SQL Server takes up to half a minute to accept connections after it starts.
Azurite listens on the standard ports, so the default `UseDevelopmentStorage=true` connection string needs no change.

### Point the tests at the containers
The tests create and migrate their own databases, so the server only needs to be running.
Set the connection strings as environment variables, which override the defaults in each test project's `appsettings.json`.
Give each test project its own databases, so that one project's tests cannot clash with another's:

| Test project | Environment variables |
|---|---|
| `Atlas.MatchingAlgorithm.Test.Integration` | `ConnectionStrings__SqlA`, `ConnectionStrings__SqlB` (a different database to `SqlA`), `ConnectionStrings__PersistentSql` |
| `Atlas.MatchingAlgorithm.Test.Validation` | `ConnectionStrings__DonorSql`, `ConnectionStrings__SqlA`, `ConnectionStrings__SqlB`, `ConnectionStrings__PersistentSql` (all may name the same database) |
| `Atlas.DonorImport.Test.Integration` | `ConnectionStrings__DonorStoreSql` |
| `Atlas.MatchPrediction.Test.Integration` | `ConnectionStrings__MatchPredictionSql` |
| `Atlas.MultipleAlleleCodeDictionary.Test.Integration` | none: it only needs Azurite |

Each connection string has the form:
`Server=localhost,14330;User ID=sa;Password=Atlas-System-Tests-1;Initial Catalog=<database>;MultipleActiveResultSets=True;TrustServerCertificate=True;`

For example, from PowerShell:

```powershell
$server = 'Server=localhost,14330;User ID=sa;Password=Atlas-System-Tests-1;MultipleActiveResultSets=True;TrustServerCertificate=True;'
$env:ConnectionStrings__SqlA = "${server}Initial Catalog=AtlasMatchingIntegrationTests;"
$env:ConnectionStrings__SqlB = "${server}Initial Catalog=AtlasMatchingIntegrationTestsB;"
$env:ConnectionStrings__PersistentSql = "${server}Initial Catalog=AtlasMatchingIntegrationTestsPersistent;"
dotnet test Atlas.MatchingAlgorithm.Test.Integration
```

To run the tests from Rider or Visual Studio, put the same variables in a `.runsettings` file, and select it as the test settings in the IDE:

```xml
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <ConnectionStrings__SqlA>Server=localhost,14330;User ID=sa;Password=Atlas-System-Tests-1;Initial Catalog=AtlasMatchingIntegrationTests;MultipleActiveResultSets=True;TrustServerCertificate=True;</ConnectionStrings__SqlA>
      <!-- ... and the other variables in the table above -->
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

The variables of a `.runsettings` file apply to every test project run with it, and several projects share names such as `SqlA`.
So with one such file, run one test project at a time: each project clears its databases when its tests start.

### Stop the containers
```bash
docker rm -f atlas-sql atlas-azurite
```

## Terraform

- Install terraform.
  - Via chocolatey: `choco install terraform --version <version number required by Atlas>`
  - Alternative installation instructions: <https://learn.hashicorp.com/terraform/getting-started/install.html>.
  - Atlas requires Terraform 1.9 or later (see `required_version` in the `versions.tf` file of the terraform project subfolder); CI and the release pipeline use 1.16.5.
  - There are guides and tools available online if you wish to have multiple versions of terraform installed on your machine.

- Install the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
  - We are *not* using Azure Cloud Shell, as it requires paid file storage to run.
  - Having installed the CLI, run `az -v` in your console of choice, to verify the installation (it takes several seconds).
  - Some people find that `az` isn't in their path, and they have to use `az.cmd` for all commands instead of just `az`.

- Log into Azure
  - Run `az login` to Log into Azure in your console.
  - You may be prompted to choose a specific subscription, if you have access to multiple.
    - Run `az account set --subscription <ID of desired Subscription>` to set this.
    - Talk to your Tech Lead about which subscription you should be using.
    - If you don't see the right subscription talk to whoever manages your Azure estate permissions.

- Run terraform
  - Still within your console, navigate to the `terraform` folder of your Atlas repository.
  - Run `terraform init` to download the relevant azure-specific plugins for our terraform configuration.
  - Run `terraform validate` to analyse the local terraform scripts and let you know if all the syntax is valid.
  - Run `terraform plan` to access Azure and determine what changes would be needed to make Azure reflect your local scripts.
    - This is the next step that my fail due to permissions issues.
    - If you get a 403 error, again speak to your Azure estate manager and ensure you have the permissions granted.