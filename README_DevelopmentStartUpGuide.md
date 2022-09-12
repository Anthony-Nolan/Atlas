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

## Running A Search
Follow these steps to get to a state in which you can run Searches against a database of donors.
The details of why these steps are necessary and what they are doing, is detailed in the rest of the core README.
It's highly recommended that you read the sections outside ZtH in parallel with it. Especially if anything doesn't make sense!

### Run Migrations
- Run EF Core Migrations for all data projects:
    - `MatchingAlgorithm.Data`, `MatchingAlgorithm.Data.Persistent`, `DonorImport.Data`, `MatchPrediction.Data`, `RepeatSearch.Data`
    - This can be done from general command line, or from the VS Package Manager Console, but in either case **must be run from within those project folders!**.
- Instructions for VS PkgMgrCons
  - Open the Nuget Package Manager Console (Menus > Tools > Nuget Package Manager > ...)
  - Run `Update-Database -project <ProjectName> -startupProject <ProjectName>` in the console.
    - Note: if you have both EF 6 and EF Core commands installed, you may need to prefix the command with `EntityFrameworkCore\` to ensure the correct version is selected for execution.
  - This should take 10-40 seconds to complete.
- Having created all the local databases, run this additional step to set up the second transient Matching algorithm database:
    - Open `Atlas.MatchingAlgorithm.Data\appsettings.json`, and modify the `ConnectionStrings.Sql` value to reference `Initial Catalog=AtlasMatchingB`.
    - Once again run Migrations for the project `Atlas.MatchingAlgorithm.Data`
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
- Monitor the `notifications` topic to ensure the import succeeded; a failure will be reported via `alerts` topic.

#### Run an Initial Data Refresh
- Open up Service Bus Explorer and connect to your local development Service Bus.
- Create the topic, `data-refresh-requests` with subscription `matching-algorithm`.
- Run `MatchingAlgorithm.Functions` and in Swagger UI (or any other API development environment) trigger the `SubmitDataRefreshRequestManual` endpoint.
  - Set `forceDataRefresh` to `true`.
  - You should get a 200 Success response almost immediately but the refresh itself will take ~15 minute to run (do NOT close the function app down until it's complete!).
- Monitor the `notifications` topic to ensure the refresh succeeded; a failure will be reported via `alerts` topic.

#### Importing Haplotype Frequency Sets
- In Storage Explorer, open your local emulator and create a new blob storage container called `haplotype-frequency-set-import`
- Edit the `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/initial-hf-set.json` file so `nomenclatureVersion` is set to the nomenclature version that was used in the data refresh.
    - The nomenclature version that was used in the data refresh can be found in SQL Server - `Atlas` -> `MatchingAlgorithmPersistent.DataRefreshHistory` and then the `HlaNomenclatureVersion` for the active database.
- Upload the json file `initial-hf-set.json` to your `haplotype-frequency-set-import` container.
    - This should take < 1 second to run.
- Open up Service Bus Explorer and connect to your local development Service Bus.
- Create the topic, `haplotype-frequency-set-import` with subscription `haplotype-frequency-import`, right-click and select `Send Message`.
    - Copy the content of the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors-hf-set-metadata.json` into the `Message Text` text box and click `Start`.
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
- Then hit the `Search` endpoint within `Atlas.Functions.PublicApi` with the content of `<gitRoot>\MiscTestingAndDebuggingResources\MatchingAlgorithm\initial-search.json` as the requests body.
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
- Some integration test suites, e.g., `Atlas.MultipleAlleleCodeDictionary.Test.Integration`, require a connection to an Azure Storage instance.
- By default, the tests will look for a local storage instance, which would require Azure Storage Emulator to be running.

## Validation Tests
The matching validation tests contact external dependencies and require connections to be configured.

- Set up user-secrets for the `Atlas.MatchingAlgorithm.Test.Validation` project.
  - Copying all secrets from the API project will work fine.
- Ensure that the Azure Storage Emulator is running.
- Ensure that you have recreated the HLA Metadata Dictionary to version `3330` locally (or have overridden the storage connection string to use a deployed environment with this
 version generated.) This can be done by hitting `{{matchingFunctionBaseUrl}}/RefreshHlaMetadataDictionaryToSpecificVersion`

## Terraform

- Install terraform.
  - Via chocolatey: `choco install terraform --version <version number required by Atlas>`
  - Alternative installation instructions: <https://learn.hashicorp.com/terraform/getting-started/install.html>.
  - Warning: you may experience issues if you try to use a newer version of terraform with Atlas; the terraform version required is listed within the `versions.tf` file of the terraform project subfolder.
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