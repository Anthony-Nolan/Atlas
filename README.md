# Nova.SearchAlgorithm
Service for AN's HSC Search Algorithm.

## Start Up Guide

####Authentication

The app is authenticated via an api key header present in requests.

The key is not checked in, so when setting up the project a file `Settings/SecureSettings` will need to be created within the `Nova.SearchAlgorithm` project.
The top level node of this project should be `<appSettings>`, and all keys specified here will be added to the config on build.

`<add key="apiKey:example-key" value="true" />` is an example of how to add a local api key.


####Storage

The service uses two storage methods for different data, SQL and Azure Cloud Tables.

- Cloud Tables
    - The `StorageConnectionString` connectionString in `Web.config` determines the connection to Azure storage. 
    The default is to use local emulated storage, for which the *Azure Storage Emulator* will need to be running. 
    - To run against development storage (e.g. if performance of the emulator is not good enough, or the emulator is 
    unavailable), this connection string can be overridden to point at the DEV storage account. (*DO NOT* check this is string in to git!)
    
- SQL
    - The service makes use of Entity Framework Code-First migrations. The models and repositories for data access
    are found within the `Nova.SearchAlgorithm.Data` project.
    - Before running the app, migrations must be run using `Update-Database`. 
    - After changing any data models, a migration must be created with `Add-Migration <migration-name>`, then run with `Update-Database`
      - **Important Note Regarding Migrations:** The `MatchingHlaAt<Locus>` tables are so large that the entity framework 
      migration runner has been known to struggle to cope with large migrations of existing data. 
     In such cases the data may need to be manually migrated
     
####Dependencies

The service has external dependencies on two services, the `DonorService` and `HlaService`. By default the configuration points to the 
deployed development instances of these service - locally the api keys for these services will need adding to the `Settings/SecureSettings` file.

## Terminology

The following terms are assumed domain knowledge when used in the code:

* Homozygous locus
  - The typings at both positions for a locus are the same
  - e.g. *A\*01:01,01:01*
* Heterozygous locus
  - The typings at both positions for a locus are not the same
  - e.g. *A\*01:01,02:01*
* GvH = Graft vs. Host 
* HvG = Host vs Graft
  - Refers to the direction of matching if one party has a homozygous locus type
  - e.g. patient (host) is *A\*01:01,02:01*; donor (graft) is *A\*01:01,01:01* - There is one mismatch im the GvH direction (but none in the HvG)
  - e.g. patient (host) is *A\*01:01,01:01*; donor (graft) is *A\*01:01,02:01* - There is one mismatch in the HvG direction, (but none in the GvH)
