# Atlas

Atlas is a "search algorithm as a service", built from Anthony Nolan's matching algorithm. 

For deployment instructions, see [README_Deployment](README_Deployment.md)

## Components

###[Donor Import](README_DonorImport.md)

Responsible for maintaining a master store of donor information. Assigns internal donor IDs, and will be used as a source of information by both the matching and 
match prediction components.

Infrastructure: 
- SQL Donor Store
- Azure blob storage for uploading source data
- Donor Import Functions App 

###[HlaMetadataDictionary](README_HlaMetadataDictionary.md)

Responsible for maintaining and accessing the dictionary of HLA Metadata - a set of tables in Azure Storage which act as an dedicated 'cache' for allele nomenclature
details published by WMDA.

Infrastructure: 
- Azure table storage Metadata store

###[Matching Algorithm](README_MatchingAlgorithm.md)

Responsible for maintaining a pre-processed store of donors, on which searches can be run - returning all donors that match input 
patient HLA according to configurable matching preferences   

Infrastructure: 
- 3x SQL Databases for storing pre-processed donor state
- 2x Functions Apps, for running searches, and maintaining data store
- Azure blob storage used for search results
- Azure service bus used for results notifications

###[Match Prediction Algorithm](README_MatchPredictionAlgorithm.md)

Responsible for maintaining a collection of haplotype frequency sets, from which match predictions can be calculated for donor/patient pairs.

Infrastructure: 
- SQL database for storing haplotype frequency data
- Azure blob storage for uploading source frequency data
- Functions app for running match prediction calculations 

###[MultipleAlleleCodeDictionary](README_MultipleAlleleCodeDictionary.md)

Responsible for maintaining and accessing Multiple Allele Code data - used for allele compression, built from source data published by NMDP.

Infrastructure: 
- Azure table storage used for storing MAC data.

### Other

#### Atlas.Common

Contains code that can be shared between multiple Atlas components. E.g. Utility methods, genetic data models. 

#### Atlas.Functions

Top level functions app, exposing public API of the Atlas system.

#### [Manual Testing](README_ManualTesting.md)

Projects dedicated to the manual, non-automated testing of various aspects of the Atlas solution.

## Zero-to-Hero Start Up Guide

### API Setup - Running A Search

Follow these steps to get to a state in which you can run Searches against a database of donors.
The details of why these steps are necessary and what they are doing, is detailed in the rest of this README below.
It's highly recommended that you read the sections below the ZtH in parallel with it. Especially if anything doesn't make sense!

- Install IDEs/Tools
  - *(All easily findable by Googling and appear to be generally happy with standard install settings.)*
  - Install a compatible IDE: VS2019 or Rider.
  - Install and Start Azure Storage Emulator.
    - Note for mac users: There is no storage emulator on mac, so instead another user-secret should be used to instead use a cloud-based storage account
    - Note that, despite it's appearance, the Emulator isn't really a service and doesn't auto-start on login by default. You may wish to configure it to do so:
      - Open Windows Startup folder: WindowsExplorer > Type `shell:startup` in the file path field.
      - Locate the Azure start command script: `StartStorageEmulator.cmd` likely found in `C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator`.
      - Right-click-and-drag the latter into the former, to create a shortcut to StorageStart script in the Startup folder.
  - Install Azure Storage Explorer.
  - Install SQL Server
  - Install Postman (or some other means of easily running HTTP requests against the API)
- Clone the repo with suitable git path settings:
  - Run `git -c core.longpaths=true clone git@github.com:Anthony-Nolan/Atlas.git`.
  - Navigate into your newly cloned repo, and run `git config core.longpaths true`.
- Run Migrations
  - Run EF Core Migrations for the `Data` and `Data.Persistent` projects. This can be done from general command line, or from the VS Package Manager Console, but in either case **must be run from within those project folders!**.
  - Instructions for VS PkgMgrCons
    - *(Note: if you have both EF 6 and EF Core commands installed, you may need to prefix the command with `EntityFrameworkCore\` to ensure the correct version is selected for execution):*
    - *The instruction "Run Migrations for `<ProjectName>`" consists of:*
      - Set Default Project (dropdown in PkgMgrConsole window) to be `<ProjectName>`.
      - Set Startup Project (Context menu of Solution Explorer) to be `<ProjectName>`.
      - Run `Update-Database` in the console.
        - This should take 10-40 seconds to complete.
    - Open the Nuget Package Manager Console (Menus > Tools > Nuget Package Manager > ...)
    - "Run Migrations for `Atlas.MatchingAlgorithm.Data.Persistent`".
    - "Run Migrations for `Atlas.MatchingAlgorithm.Data`".
    - Open `Atlas.MatchingAlgorithm.Data\appsettings.json`, and modify the `ConnectionStrings.Sql` value to reference `Initial Catalog=AtlasMatchingB`.
    - "Run Migrations for `Atlas.MatchingAlgorithm.Data`". *(again)*
    - Open your local SQL Server and verify that you now have 3 databases: `AtlasMatchingPersistent`, `AtlasMatchingA` and `AtlasMatchingB`.
- Compile and Run API
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

  - In that `secrets.json` file configure:
    - the appropriate ServiceBus connection strings
      - Acquire the connection strings for the Azure ServiceBus being used for local development, from the Azure Portal.
        - The connection strings can be found under the `Shared Access Policies` of the ServiceBus Namespace
        - We use the same ServiceBus Namespace for both connections, but with differing access levels.
      - Create a `MessagingServiceBus.ConnectionString` setting using the `read-write` SAP.
      - Create a `NotificationsServiceBus.ConnectionString` setting using the `write-only` SAP.
        - *Note these keys aren't part of the `Client` block of the settings object!*
- Set up sensible initial data.
  - In SSMS, open and run the SQL script `<gitRoot>\MiscTestingAndDebuggingResources\MatchingAlgorithm\InitialRefreshData.sql"`.
    - This should take < 1 second to run.
  - In the Swagger UI, trigger the `HlaMetadataDictionary > recreate-active-version` endpoint.
    - This can take several minutes to run.
  - In the Swagger UI, trigger the `Data Refresh > trigger-donor-import` endpoint.
    - This should take < 1 minute to run.
  - In the Swagger UI, trigger the `Data Refresh > trigger-donor-hla-update` endpoint.
    - This should take 1-2 minutes to run.
- Run a search (avoiding NMDP Code lookups).
  - Restart the API project, and use Swagger to POST the JSON in `<gitRoot>\MiscTestingAndDebuggingResources\MatchingAlgorithm\ZeroResultsSearch.json` to the  `/search` endpoint.
  - You should get a 200 Success response, with 0 results.
  - The first search should take 20-60 seconds.
  - Subsequent searches should take < 1 second.
- Run a search that uses the NMDP Code lookup API.
  - Restart the API project, and POST the JSON in `<gitRoot>\MiscTestingAndDebuggingResources\MatchingAlgorithm\EightResultsSearch.json` to the  `/search` endpoint.
  - You should get a 200 Success response, with 8 results.
  - The first search should take 40-90 seconds.
  - Subsequent searches should take < 1 second.

### Validation Tests

The above steps should be sufficient to run all the unit tests. e.g. `Atlas.MatchingAlgorithm.Test` and `Atlas.MatchingAlgorithm.IntegrationTest`
The end-to-end tests, however, contact external dependencies, and require connections to be configured.

- Set up user-secrets for the `Atlas.MatchingAlgorithm.Test.Validation` project.
  - Re-use the values used to run the API project.
  - The specific values needed are:
    - `NotificationsServiceBus.ConnectionString`
  - But just copying all secrets from the API project will work fine.
- Ensure that the Azure Storage Emulator is running

### Terraform

- Install terraform.
  - If you have chocolatey installed then `choco install terraform`.
  - If not: <https://learn.hashicorp.com/terraform/getting-started/install.html>. Video is Linux/Mac, then Windows in the 2nd half.
- Install the Azure CLI: <https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest>
  - We are *not* using Azure Cloud Shell, as it requires paid file storage to run.
- Having installed the CLI, run `az -v` in your console of choice, to verify the installation (it takes several seconds)
  - Some people find that `az` isn't in their path, and they have to use `az.cmd` for all commands instead of just `az`.
- Run `az login` to Log into Azure in your console.
- You may be prompted to choose a specific subscription, if you have access to multiple.
  - Run `az account set --subscription <ID of desired Subscription>` to set this.
    - Talk to your Tech Lead about which subscription you should be using.
  - If you don't see the right subscription talk to whoever manages your Azure estate permissions.
- Still within your console, navigate to the `terraform` folder of your Atlas repository.
- Run `terraform init` to download the relevant azure-specific plugins, for our terraform configuration.
- Run `terraform validate`, to analyse the local terraform scripts and let you know if all the syntax is valid.
- Run `terraform plan`, to access Azure and determine what changes would be needed to make Azure reflect your local scripts.
  - This is the next step that my fail due to permissions issues.
  - If you get a 403 error, again speak to your Azure estate manager and ensure you have the permissions granted.

### Functions
  
Getting functions to run locally with Swagger for VS2019:

- To run a function set it as your start-up project and run it.
- (if Swagger has been configured for that FunctionApp) the Swagger web addresses should be shown in the list of Http Functions.
- Simply copy this address into your browser and you will be able to trigger the functions from there.
- Trigger the HealthCheck endpoint in Swagger to make sure the function is working correctly.

## More detailed Notes

### Local Settings

#### Non Azure-Functions Settings

Settings for each non-functions project are defined in the `appsettings.json` file.

In some cases these settings will need overriding locally - either for secure values (e.g. api keys), or if you want to use a different service (e.g. azure storage account, service bus)

This is achieved with User Secrets: <https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-2.2&tabs=windows>

#### Azure-Functions Settings

Azure functions requires a different settings configuration. With the "Values" object in `local.settings.json`, it expects a collection of string app settings - these reflect a 1:1 mapping with the app settings configured in Azure for deployed environments

> Warning! Attempting to use nested objects in this configuration file will prevent the app settings from loading, with no warning from the functions host

In order to allow checking in of non-secret default settings, while dissuading accidental check-in of secrets, the following pattern is used: 

- A `local.settings.template.json` is checked in with all default settings values populated. 
    - Any new app settings should be added to these files
    - Any secrets (e.g. service bus connection strings) should be checked in with an obviously dummy value (e.g. override-this)
- On build of the functions projects, this template will be copied to a gitignored `local.settings.json`
    - This file can be safely edited locally to override any secret settings without risk of accidental check-in
    - This copying is done by manually amending the csproj and adding the following code: 
    ```
      <Target Name="Scaffold local settings file" BeforeTargets="BeforeCompile" Condition="!EXISTS('$(ProjectDir)\local.settings.json')">
          <Copy SourceFiles="$(ProjectDir)\local.settings.template.json" DestinationFiles="$(ProjectDir)\local.settings.json" />
      </Target>
    ```
- When someone else has added new settings, you will need to either: 
    - (a) Add the new setting manually to `local.settings.json`
    - (b) Delete `local.settings.json` and allow it to regenerate on build. Any local secret settings will then need to be re-applied. 
        - *Warning* When deleting through an IDE, it may remove the "Copy always" functionality in the csproj, at which point the file 
        will not be copied to the build folder. 
        - Either delete via file explorer, or remember to mark as "copy always" on recreation

*Warning* - when running a functions app for the first time on a machine, this copying may not have happened in time. If no settings are found, try rebuilding and running again. 


#### Options Pattern

To enable a shared configuration pattern across both the functions project, and api used for testing, a modification of the 
[Options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2) is used:

- Top level entry-points (e.g. functions apps, ASP.NET API) are responsible for providing the expected settings to logical component projects.
    - Within these apps, the standard options pattern is followed, by registering settings as `IOptions<TSettings>`
- Within the component projects, settings are re-registered as `TSettings` directly
    - This allows for decoupling of the components and the source of their settings
    - Do not attempt to re-register `IOptions<TSettings>` within component projects, or declare a dependency on them 

### Storage

The service uses two storage methods for different data, SQL and Azure Cloud Tables.

- Cloud Tables
  - The `StorageConnectionString` setting in `appsettings.json` determines the connection to Azure storage.
    The default is to use local emulated storage, for which the *Azure Storage Emulator* will need to be running.
  - To run against development storage (e.g. if performance of the emulator is not good enough, or the emulator is unavailable), this connection string can be overridden using user secrets to point at the DEV storage account. (*DO NOT* check this is string in to git!)
- SQL
  - The service makes use of Entity Framework (EFCore) Code-First migrations. The models and repositories for data access
    are found within the `Atlas.*.Data` (and for the matching component, also `Atlas.MatchingAlgorithm.Data.Persistent`) projects.
  - Before running the app, migrations must be run using `dotnet ef database update -p <projectName>` from a terminal (or `Update-Database` from the nuget package manager)
  - After changing any data models, a migration must be created with `dotnet ef migrations add -p <projectName>` (or `Add-Migration <migration-name>` in nuget package manager), then run as above


# README Index

Due to the size and complexity of the project, the README has been split into various small chunks. Other READMEs have been linked where appropriate, but here is a 
comprehensive list:

- [Core README (You Are Here)](README.md)
- [Deployment](README_Deployment.md)
- Components 
    - [Donor Import](README_DonorImport.md) 
    - [HLA Metadata Dictionary](README_HlaMetadataDictionary.md) 
    - [Matching Algorithm](README_MatchingAlgorithm.md)
        - [Validation Test (Non-Technical BDD Testing)](Atlas.MatchingAlgorithm.Test.Validation/ValidationTests/Features/README_MatchingValidationTests.md)
    - [Match Prediction Algorithm](README_MatchPredictionAlgorithm.md) 
    - [MAC Dictionary](README_MultipleAlleleCodeDictionary.md)
- [Manual Testing](README_ManualTesting.md)
- [Test and Debug Resources](MiscTestingAndDebuggingResources/README_TestAndDebug.md)
- [Architectural Decision Record](ArchitecturalDecisionRecord/README_ArchitecturalDecisionRecord.md)