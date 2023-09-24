# Summary

The HLA Metadata Dictionary (HMD) Library is responsible for knowing how to interpret HLA descriptor strings in various forms; the "HLA Nomenclature".

## Data sources
The HMD gets its original data from files published by IMGT/HLA (primarily, the "WMDA" files), plus some data about MACs published by the NMDP.

On command (expected to be roughly quarterly), the HMD library will read the latest version of the HLA Nomenclature (read from a mirror hosted by Anthony Nolan Bioinformatics).
Having read it from raw data files, the HMD converts it into a the format that is of most use for itself, and slow-caches all of that formatted data in its own Azure CloudStorage Table.

## Versioning
The HMD is automatically regenerated to the latest IMGT/HLA version on every [data refresh,](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#data-refresh) but can also hold multiple older IMGT/HLA versions. The main use case for this is the need to upload haplotype frequency set files that have been encoded to an older HLA version.
[See integration README for instructions on how to regenerate the HMD to an older version](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#hla-metadata).

## Allele groups
These are the different allele grouping that the HMD currently handles:
- G group: alleles that share the same DNA sequence at the ABD (Antigen Binding Domain) region.
- P group: alleles that share the same protein sequence at the ABD region - excludes null alleles, as they do not result in a protein.
- Small g group: alleles that share the same protein sequence at the ABD region - includes null alleles.

P group and G group definitions are directly extracted from the WMDA files.
There is currently no WMDA file for small g groups; instead they are built by logic held in the HMD using P group and G group data.

Due to the use of the camel-case in the codebase, the word "small" is used to differentiate between the two types of G group.
I.e., "GGroup" without the qualifier refers to the nucleotide-level group, and "SmallGGroup" refers to the protein-level group.

## Caching
When used, the HMD then caches the contents of its CloudTables in memory, although that initial caching is slightly slow. Cache-Warming methods are available to trigger that cache-to-memory action, if desired.

The run-time consumer of the HMD doesn't need to know much of those details though; all it needs to know is that the HMD will return data from an in-memory cache, and that if you want to ensure the first usage of the HMD is already fast, then you can pre-warm the cache.

## Project-To-Project Interface

The logic  classes that other projects should be using are the following.

* `ServiceConfiguration.RegisterHlaMetadataDictionary()`
  * Used during App `Startup` to configure dependency injection for the classes you'll use.
  * This method demands various configurationSettings be passed in. See below for details.
* `IHlaMetadataDictionaryFactory`
  * **This is the ONLY interface that consuming classes should be directly declaring depending upon at general run-time, using DI.**
  * `.BuildDictionary(string activeHlaNomenclatureVersion)`
  * `.BuildCacheControl(string activeHlaNomenclatureVersion)`
  * This is the object that your classes will depend upon, and which will be injected by DI.
  * The two build methods require that you pass in the version of the HLA Nomenclature that you want a Dictionary for.
    * A Dictionary only refers to a single Nomenclature version, but all dictionaries of the same version will be the same object.
    * Likewise the CacheControl for version "X" will be the control affecting the Dictionary of version "X".
    * These versions should match the WMDA Nomemclature version strings, with the separating dots removed. e.g. "3330".
* `IHlaMetadataDictionary`
  * This is the primary class that you will actually use. You will likely depend on the `Factory`, and immediately `.Build()` a dictionary in your `ctor` and keep hold of the output `Dictionary` for use.
  * This object exposes all of the methods for looking up details of a given HLA string.
* `IHlaMetadataCacheControl`
  * As referenced earlier, the HMD caches all its data in memory. By default the first call will cache the relevant data it needs, which can be quite slow. If you want to ensure that your first usage of the HMD is quick then you can use the `CacheControl` to pre-warm the memory caches.
  * Either warm everything, with `.PreWarmAllCaches()` or just target `.PreWarmAlleleNameCache()` if desired.
  
## Configuration

During DI setup, you should call `ServiceConfiguration.RegisterHlaMetadataDictionary(...)`.

That method has several parameters declaring what the HMD Library needs to know / have access to. Beyond those things explicitly declared by the parameters, the HMD library does not require or assume any other context or settings.

The Parameters are all of the form: `Func<IServiceProvider, T>`. They will be passed to the service registration code, so that the values in question can be fetched at the point that the types need to be instantiated.

* This allows you to provide the parameters as AppSettings references, e.g. through `IOptions<>` but declaring and locating those AppSettings is the responsibility of the calling project.
* Equally, if you have the literal values to hand, you could simply pass the `Func<>` as `(_) => "myConfigValue"`.

### Config Parameters

* `fetchHlaMetadataDictionarySettings` (`HlaMetadataDictionarySettings`)
  * Settings needed to run the HMD. Values include:
    * A Connection string to the Azure CloudStorage account that the HMD should use to write its data when regenerating the Dictionary, and thus to read from when using the data.
    * The Base URI of the WMDA data source, to be used when regenerating data.
* `fetchApplicationInsightsSettings` (`ApplicationInsightsSettings`)
  * Settings needed to log to application insights 
  * The HMD assumes that it is being run in Azure with an ApplicationInsight system attached. It will identify that system automatically, but you must provide the default LoggingLevel. Likely "Info".
* `fetchHlaClientBaseUrl` (`MacDictionarySettings`)
  * Settings needed to talk to the MacDictionary, used to expand MACs (multiple allele codes)
    
### Long directories

Some of the files in the hla metadata dictionary tests are longer than the 260 character limit that Windows expects. This causes problems in the IDE and in Git for Windows:

- For your IDE, use Visual Studio 2019, or Rider, as there are known issues trying to load the Test project in Visual Studio 2017 due to this. The issue is not present in more recent IDEs.
- For your git, we use the `core.longpaths` git setting, as described in the zero-to-hero section in the [core Readme](README.md). If you cloned your repo prior to reading this README, it would be wise to delete and reclone it with the modified git setting.


## Extending the HMD with new Lookups

### Background
Originally, the HMD was internal to the matching algorithm project, and the services were designed to lookup the HLA metadata needed to perform matching and scoring.
These services were later extracted to their own dedicated project, with a public external interface accessible to any Atlas project,
and the HMD has been extended with lookups required by other features, such as the match prediction algorithm.

In terms of design, this means that some services within the HMD remain coupled to matching and scoring, whereas the newer services are de-coupled to allow usage by any consumer.
There are cards in the Atlas backlog to clean up the HMD (ATLAS-394), but in the mean time, here is some guidance for how to add new lookups in the context of the existing design.

### Design
Logic within the HMD can be split into the following categories:

#### WMDA Data Extraction
Retrieval and basic parsing of IMGT/HLA database nomenclature files (of a specified version) to produce the "WMDA dataset".

#### Data Generation
Processing and manipulation of the WMDA dataset to produce metadata collections optimised for key-value lookups.
- Each collection is persisted to its own cloud table, named after the data it contains and the version number.
- Orchestrator services are used to ensure the recreation of each metadata collection when requested, e.g., after the publication of a new version of the HLA nomenclature files.
- In most cases, the "key" is usually the allele or serology name, as it appears in the WMDA `hla_nom` file.
    - For alleles, keys will also be generated for:
        - the "two-field" name variant, as seen in decoded MACs, e.g., `01:AB` decodes to `01:01/01:02`;
        - and the expected XX code, e.g., `01:XX`.
        - The metadata values for these variants are consolidated so the final collection only contains one row per unique key name.
- Note, however, that the key could be any valid HLA type.
  - E.g., for `SmallGGroupToPGroupLookup`, the key is a small g group name, and the metadata value returned is the equivalent P group.

#### Data Retrieval

##### Metadata Lookup
Locus, hla name, and version number is used to lookup the required metadata from the appropriate collection.
- There is more than one HMD lookup pattern, dictated by the kind of HLA data submitted.
- Where a lookup service is expected to handle any supported HLA typing category, e.g., the matching metadata lookup:
  - The submitted HLA typing will be categorised by name;
  - The hla name is processed to provide the keys needed to search the appropriate metadata collection:
    - for a molecular typing, the HLA is expanded to a list of allele names, as dictated by the typing category;
    - for serology, no such expansion is required; the submitted name is sufficient.
  - Retrieved metadata values are consolidated before being returned to the consumer.
- Where the lookup service is only expected to handle one HLA typing category, e.g., GGroupToPGroup.
  - No expansion occurs; the submitted value will be used to search the metadata collection directly.
- In all cases, HMD exceptions will be thrown if a HLA name is either invalid, or no value is found in the metadata collection.

##### HLA Conversion
HLA conversion relies on the same lookup services described above, but provides a subtly different API to consumers.
- The HLA converter is used when the consumer wishes to directly convert between two HLA typing categories, e.g., small g group to P group.
- Not every possible combination of HLA typing categories is currently supported; options are limited.

### Examples of Previous Lookups
Adding a new lookup path to the HMD involves changing multiple files; the best way to ensure all necessary changes have been made is to emulate previous commits.

- Lookup that should handle any supported HLA category: search git history for `ATLAS-879` (lookup the equivalent small g groups for any support HLA category).
- HLA converter/lookup that handles one HLA typing category: search git history for `ATLAS-880` (convert small g group to its equivalent P group).
