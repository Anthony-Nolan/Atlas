# Architectural Overview

Atlas is split up into components, each with its own responsibilities and resources. This document provides an overview of Atlas architecture along with links to documentation dedicated to individual components.

Further architectural documentation:
- [Architecture Diagrams](./ArchitectureDiagrams/README_Diagrams.md)
- [Architectural Decision Record](./ArchitecturalDecisionRecord/README_ArchitecturalDecisionRecord.md)

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

## Storage

The service uses two storage methods for different data, SQL and Azure Cloud Tables.

### SQL
  - The service makes use of Entity Framework (EFCore) Code-First migrations.
  - The models and repositories for data access are found within the `Atlas.*.Data` (and for the matching component, also `Atlas.MatchingAlgorithm.Data.Persistent`) projects.
  - Before running the app, migrations must be run using `dotnet ef database update -p <projectName>` from a terminal (or `Update-Database` from the nuget package manager)
  - After changing any data models, a migration must be created with `dotnet ef migrations add -p <projectName>` (or `Add-Migration <migration-name>` in nuget package manager), then run as above.

## Other Solution Projects/Functions

### Atlas.Client.Models

Models used by the public API.

### Atlas.Common

Contains code that can be shared between multiple Atlas components. E.g. Utility methods, genetic data models. 

### Atlas.Common.Public.Models

Only contains those models that are commonly used across the solution AND are also referenced by the Atlas client models.
These models were originally part of the main `Atlas.Common` project, but were moved to their own class library so they can be published with the client models as a NuGet package.

### Atlas.Functions

Top level functions app, responsible for: 
- Running MAC import 
- Orchestrating match prediction for finished matching results

### Atlas.Functions.PublicApi

Top level functions app, exposing public API of the Atlas system.

### [Manual Testing](README_ManualTesting.md)

Projects dedicated to the manual, non-automated testing of various aspects of the Atlas solution.