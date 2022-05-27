# Atlas

**Atlas is licensed under the GNU GPL-v3.0 (or later) license. For details see the [license file](LICENSE).**

Atlas is a "search algorithm as a service", extended from Anthony Nolan's matching algorithm. 

For deployment instructions, see [README_Deployment](README_Deployment.md)

## README Index

Due to the size and complexity of the project, the README has been split into various small chunks. Other READMEs have been linked where appropriate, but here is a 
comprehensive list:

- [Core README (You Are Here)](README.md)
- [Development Start Up Guide ("Zero To Hero")](README_DevelopmentStartUpGuide.md)
- [Architecture Diagrams](ArchitectureDiagrams/README_Diagrams.md)
- [Contribution and Versioning](README_Contribution_Versioning.md)
- Guide for Installation and usage of the ATLAS system 
    - [Deployment](README_Deployment.md) - deploying the resources needed to run Atlas
    - [Configuration](README_Configuration.md) - configuring resources / settings of Atlas to fine tune your installation
    - [Integration](README_Integration.md) - processing reference data, and integrating your non-Atlas systems into Atlas
    - [Support](README_Support.md) - resources for assisting with any issues when running an installation of Atlas
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


## Versioning & CHANGELOGs
- See [Version ADR](ArchitecturalDecisionRecord/Phase2/005-Versioning.md) for an explanation of Atlas versioning.

### CHANGELOG Index
- [Client](Atlas.Client.Models/CHANGELOG_Client.md)
- [Atlas Product](Atlas.Functions.PublicApi/CHANGELOG_Atlas.md)


## Components

### [Donor Import](README_DonorImport.md)

Responsible for maintaining a master store of donor information. Assigns internal donor IDs, and will be used as a source of information by both the matching and 
match prediction components.

Infrastructure: 
- SQL Donor Store
- Azure blob storage for uploading source data
- Donor Import Functions App 

### [HlaMetadataDictionary](README_HlaMetadataDictionary.md)

Responsible for maintaining and accessing the dictionary of HLA Metadata - a set of tables in Azure Storage which act as an dedicated 'cache' for allele nomenclature
details published by WMDA.

Infrastructure: 
- Azure table storage Metadata store

### [Matching Algorithm](README_MatchingAlgorithm.md)

Responsible for maintaining a pre-processed store of donors, on which searches can be run - returning all donors that match input 
patient HLA according to configurable matching preferences   

Infrastructure: 
- 3x SQL Databases for storing pre-processed donor state
- 2x Functions Apps, for running searches, and maintaining data store
- Azure blob storage used for search results
- Azure service bus used for request queuing and results notifications

### [Match Prediction Algorithm](README_MatchPredictionAlgorithm.md)

Responsible for maintaining a collection of haplotype frequency sets, from which match predictions can be calculated for donor/patient pairs.

Infrastructure: 
- SQL database for storing haplotype frequency data
- Azure blob storage for uploading source frequency data
- Functions app for running match prediction calculations 

### [MultipleAlleleCodeDictionary](README_MultipleAlleleCodeDictionary.md)

Responsible for maintaining and accessing Multiple Allele Code data - used for allele compression, built from source data published by NMDP.

Infrastructure: 
- Azure table storage used for storing MAC data.

### Other

#### Atlas.Common

Contains code that can be shared between multiple Atlas components. E.g. Utility methods, genetic data models. 

#### Atlas.Functions

Top level functions app, responsible for: 
- Running MAC import 
- Orchestrating match prediction for finished matching results

#### Atlas.Functions.PublicApi

Top level functions app, exposing public API of the Atlas system.

#### [Manual Testing](README_ManualTesting.md)

Projects dedicated to the manual, non-automated testing of various aspects of the Atlas solution.

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