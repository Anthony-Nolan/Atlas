## Zero-to-Hero Start Up Guide

### API Setup - Running A Search

Follow these steps to get to a state in which you can run Searches against a database of donors.
The details of why these steps are necessary and what they are doing, is detailed in the rest of the core README.
It's highly recommended that you read the sections outside ZtH in parallel with it. Especially if anything doesn't make sense!

- Install IDEs/Tools
  - *(All easily findable by Googling and appear to be generally happy with standard install settings.)*
  - Install a compatible IDE: VS2019 or Rider.
  - Install and Start Azure Storage Emulator.
    - Note for mac users: There is no storage emulator on mac, so instead another user-secret should be used to instead use a cloud-based storage account
    - Note that, despite it's appearance, the Emulator isn't really a service and does not auto-start on login by default. You may wish to configure it to do so:
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
  - Run EF Core Migrations for all data projects:
        - `MatchingAlgorithm.Data`, `MatchingAlgorithm.Data.Persistent`, `DonorImport.Data`, `MatchPrediction.Data` 
        - This can be done from general command line, or from the VS Package Manager Console, but in either case **must be run from within those project folders!**.
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
  - Importing Donors.
    - In Storage Explorer, open your local emulator and create a new blob storage container called `donors`.
    - Upload the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors.json` to your `donors` container.
      - This should take < 1 second to run.
    - Open up Service Bus Explorer and connect to your development Service Bus. (Make sure your local settings are set up accordingly)
      - There is no emulator for azure service bus, so a new bus will need to be manually set up in Azure for use when developing locally.
    - Right click on the `donor-import-file-uploads` topic and select `Send Message`.
      - Copy the content of the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors-data.json` into the `Message Text` text box and click `Start`.
    - When you now run `DonorImport.Functions` your local Donors table should now be populated.
      - This should take < 5 minute to run.
    - Once that's been run you should now run `MatchingAlgorithm.Functions.DonorManagement` to allow the active transient database to be populated.
      - This should take < 1 minute to run.
  - Running Initial Data Refresh.
    - Run `MatchingAlgorithm.Functions` and in Swagger UI (or any other API development environment) trigger the `ForceDataRefresh` endpoint.
      - This should take < 15 minute to run.
  - Importing Haplotype Frequency Sets.
    - In Storage Explorer, open your local emulator and create a new blob storage container called `haplotype-frequency-set-import`
    - Edit the `<gitRoot>/MiscTestingAndDebuggingResources/MatchPrediction/initial-hf-set.json` file so `nomenclatureVersion` is set to the nomenclature version that was used in the data refresh.
      - The nomenclature version that was used in the data refresh can be found in SQL Server - `AtlasMatchingPersistent` -> `MatchingAlgorithmPersistent.DataRefreshHistory` and then the `HlaNomenclatureVersion` for the active database.
    - Upload the json file `initial-hf-set.json` to your `haplotype-frequency-set-import` container.
      - This should take < 1 second to run.
    - Open up Service Bus Explorer and connect to your development Service Bus.
    - Right click on the `haplotype-frequency-set-import` topic and select `Send Message`.
      - Copy the content of the json file `<gitRoot>/MiscTestingAndDebuggingResources/DonorImport/initial-donors-hf-set-data.json` into the `Message Text` text box and click `Start`.
    - When you now run `MatchPrediction.Functions` your local HaplotypeFrequencies and HaplotypeFrequencySets tables should now be populated.
      - This should take < 5 minute to run.
- Run a search (avoiding MAC lookups).
  - In Storage Explorer, open your local emulator and create two new blob storage containers called `atlas-search-results` and `matching-algorithm-results`.
  - Run `Atlas.Functions.PublicApi`, `Atlas.Functions`, and `Atlas.MatchingAlgorithm.Functions` all in parallel.
    - To do this in VisualStudio first set your default project to `Atlas.Functions.PublicApi` and then run it.
    - Then in solution explorer right click on both `Atlas.Functions`, and `Atlas.MatchingAlgorithm.Functions` and select `Debug -> Start New Instance`.
    - Alternatively you can right click on your solution in Solution Explorer go to properties and under multiple start up projects select the three functions.
    - *You will want to make sure all the local settings for these functions are up to date*
  - Then hit the `Search` endpoint within `Atlas.Functions.PublicApi` with the content of `<gitRoot>\MiscTestingAndDebuggingResources\MatchingAlgorithm\initial-search.json` as the requests body.
  - You should get a 200 Success response. In the `atlas-search-results` blob storage you have created you should have a file containing the search results.
  - The first search should take 20-60 seconds.
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
