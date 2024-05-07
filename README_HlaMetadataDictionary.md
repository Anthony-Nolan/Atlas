# Summary

The HLA Metadata Dictionary (HMD) Library is responsible for knowing how to interpret HLA descriptor strings in various forms, i.e., the "HLA Nomenclature".

The HMD gets its original data from files published by IMGT/HLA (primarily, the "WMDA" files).

On command, the HMD library will read the latest version of the HLA Nomenclature (by default, from a [mirror hosted by Anthony Nolan Bioinformatics](https://github.com/ANHIG/IMGTHLA)).
Having read it from raw data files, the HMD converts it into the format that is of most useful for itself, and slow-caches all of that formatted data in its own Azure CloudStorage Table.

## Table of Contents
- [Feature Documentation](#feature-documentation)
- [Technical Documentation](#technical-documentation)

## Feature Documentation

### Supported HLA categories
- Molecular:
  - Allele
  - Allele string with format of either `99:01/02/05` or `99:01:01/99:02:03/100:05`
  - [Allele group](#allele-groups) (P/G/"Small g")
  - MAC (a.k.a. "NMDP code")
  - XX code
- Serology  

#### Allele groups
These are the different allele grouping that the HMD currently handles:
- G group: alleles that share the same DNA sequence at the ABD (Antigen Binding Domain) region.
- P group: alleles that share the same protein sequence at the ABD region - excludes null alleles, as they do not result in a protein.
- Small g group: alleles that share the same protein sequence at the ABD region - includes null alleles.

Due to the use of the camel-case in the codebase, the word "small" is used to differentiate between the two types of G group.
I.e., "GGroup" without the qualifier refers to the nucleotide-level group, and "SmallGGroup" refers to the protein-level group.

P group and G group definitions are directly extracted from the WMDA files, `hla_nom_p` and `hla_nom_g`, respectively.

There is no WMDA file for small g groups; instead they are built by logic held in the HMD using P group and G group data:
* For each P group `p`:
  * Identify null alleles that share the same ABD by expanding `p` to a list of G groups, and extracting the null alleles from these G group definitions.
  * Combine the null allele list with list of expressing alleles that map to `p`.
  * Name the new group:
    * If all alleles in final list share the same first two fields (`999:999`), group will be named `999:999`.
    * Else, existing name of P group `p` will be used, replacing the `P` suffix with a lowercase `g`, where relevant.
  * Final data object will contain: locus, name of small g group, list of alleles mapped to the group, name of the equivalent P group.
* Select null alleles that do not share their ABD sequence with a P group:
  * Select alleles that map to G groups whose allele list does _not_ intersect with any P group.
  * For each locus:
    * Group these alleles by their first two fields (`999:999`).
    * Name each group after its key, `999:999`, adding an expression suffix if the group's allele count is 1 and the single allele has a suffix, i.e., `999:999N`.
  * The final data object will contain: locus, name of small g group, and list of alleles mapped to the group (equivalent P group field will be empty).

### HLA Conversion
The HMD holds metadata about a variety of HLA typing categories, and provides the means to convert between them.

Examples:
- Donor search and matching are done at the P group level, as a P-group match is the minimum requirement for two typings to be considered allele-level matched ([deemed "AI3" in the WMDA matching framework](https://www.nature.com/articles/bmt2010132)).
  - During data refresh, donor typings are converted from their original HLA values to equivalent P groups.
  - During search, the patient typing is also converted to P groups, and they are used to query the donor database to find all potential matches.
- Match prediction:
  - Subject typing is converted to the equivalent allele groups (P/G/"small g") to find the haplotypes that explain them and build a set of possible genotypes.
  - The possible genotypes are then converted to P groups to allow for match calculation between the patient and donor genotype sets.

HLA metadata held in the following IMGT/HLA files form the foundation of most HLA conversions:
- `hla_nom_g` - for conversion from allele to G group, and vice versa
- `hla_nom_p` - for conversion from allele to P group, and vice versa
- `rel_dna_ser` - for conversion from serology to allele, and vice versa

Combining this with other HLA metadata and logic allows for further conversions paths, e.g., from serology to P group, from MAC to small g group, etc.

#### Conversion between Serology and Molecular typings
As described above, there are many occasions within search where a serology typing needs to be converted to its equivalent molecular typing. Molecular typings also need to be converted back to serology in order to determine antigen match status.

<br/>
During HMD construction, the following logic is used to find the molecular typings that map to a serology typing:

```
Given a serology of `s`:

1. Find all serologies that match to `s` in IMGT/HLA file, rel_ser_ser, resulting in list of "matching serologies".
2. For every matching serology, `ms`, find alleles that have been assigned to `ms` within the rel_dna_ser file; the combined result will be a list of "matching alleles".
3. For every matching allele, `ma`, find:
  - "Matching P group" from hla_nom_p file (where `ma` is an expressing allele).
  - "Matching G group" from hla_nom_g file.
  - "Matching small g group" from the constructed small g lookup table in the HMD.
```
At the end of this process, every possible `s` within the HMD will have a list of matching serologies, alleles, P groups, G groups and small g groups.

The reverse process of mapping from molecular typing to serology is much simpler, as it only requires looking up the relevant allele(s) in `rel_dna_ser` file and saving the returned assignment(s).

The logic above shows that the assignments within the `rel_dna_ser` file are the primary determinant of how a serology typing is interpreted, and in turn, why a given serology-typed donor is brought back for a molecular-typed patient, or why a molecular-typed donor is called an antigen match or mismatch.

Serology assignments fall into the following categories:
  - "Expected", where the allele maps to either:
    1. the antigen found in the allele's name, e.g., `A*01:01:01:01` maps to `A1`, `B*51:03` maps to `B5103`.
    2. the split of the allele's first field, e.g., `B*15:01:01:01` -> `B62`, where `B62` is a split of `B15`.
    3. the broad of the allele's first field, e.g., `B*38:09` -> `B16`, where `B16` is the broad of `B38`.
  - "Unexpected" - the allele is assigned to an antigen that is outside of the family indicated by the allele's first field, e.g., `B*15:271` maps to both `B15` and `B70`, the latter being the unexpected assignment, as it is not part of the `B15` broad group.

Of the above assignment categories, the ones that tend to lead to the most unexpected matches are "Expected-3" and "Unexpected":
- `Expected-3` assignments may bring back a donor that is typed with a split of the broad that the allele is mapped to. E.g., a `B*38:09` typed patient may bring back a donor typed with `B39` via the `B16` assignment. This may cause confusion as ordinarily `B38` and `B39` are antigen mismatched ([deemed "SD2" in the WMDA matching framework](https://www.nature.com/articles/bmt2010132)).
- `Unexpected` assignments may bring back a donor that is typed with a serology that is completely outside the expected broad group. E.g., a `B*15:271` typed patient may bring back a donor typed with `B70` via the `B70` assignment. This may cause confusion as `B15` and `B70` are antigen mismatched ([deemed "SD3" in the WMDA matching framework](https://www.nature.com/articles/bmt2010132)).
- The potential for confusion is exacerbated when the responsible allele is hidden within an ambiguous typing, such as a MAC or XX code.
- [See this ticket](https://github.com/Anthony-Nolan/Atlas/issues/1076) for examples that were questioned by search coordinators during UAT. Note to developers/support agents: the debug endpoint on the matching algorithm functions app, `debug/SerologyToAlleleMapping`, can help determine the cause for unexpected serology-DNA matches, as was done in the ticket.

Within the existing implementation of Atlas, the only way to prevent the return of confusing matches would be to reference a curated list of acceptable (or alternatively, blacklisted) `rel_dna_ser` assignments. This curation exercise requires clinical oversight and would need to be reviewed after every HLA nomenclature release.

The Anthony Nolan development team that decided the core rules for matching and HLA interpretation decided that the cost of implementing and maintaining this solution would be much higher than the work-around of training search coordinators to be aware of how the algorithm works. Especially considering that many registries have minimum typing standards for patients that exclude serology, and serology-typed donors are generally less desirable than molecular typed donors due to both their typing ambiguity and age.

Therefore, at present there is no plan to curate `rel_dna_ser` assignments. However, other "smaller" code-based solutions may be considered, if proposed.



## Technical Documentation

### Versioning
The HMD is automatically regenerated to the latest IMGT/HLA version on every [data refresh,](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#data-refresh) but can also hold multiple older IMGT/HLA versions. The main use case for this is the need to upload haplotype frequency set files that have been encoded to an older HLA version.
[See integration README for instructions on how to regenerate the HMD to an older version](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#hla-metadata).

To see what versions of the HMD are currently available, you can run the following SQL query on the shared Atlas database:

```sql
SELECT DISTINCT HlaNomenclatureVersion
FROM MatchingAlgorithmPersistent.DataRefreshHistory
WHERE WasSuccessful = 1
ORDER BY HlaNomenclatureVersion DESC
```

### Caching
When used, the HMD then caches the contents of its CloudTables in memory, although that initial caching is slightly slow. Cache-Warming methods are available to trigger that cache-to-memory action, if desired.

The run-time consumer of the HMD doesn't need to know much of those details though; all it needs to know is that the HMD will return data from an in-memory cache, and that if you want to ensure the first usage of the HMD is already fast, then you can pre-warm the cache.

### Project-To-Project Interface

The logic classes that other projects should be using are the following.

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
  
### Configuration

During DI setup, you should call `ServiceConfiguration.RegisterHlaMetadataDictionary(...)`.

That method has several parameters declaring what the HMD Library needs to know / have access to. Beyond those things explicitly declared by the parameters, the HMD library does not require or assume any other context or settings.

The Parameters are all of the form: `Func<IServiceProvider, T>`. They will be passed to the service registration code, so that the values in question can be fetched at the point that the types need to be instantiated.

* This allows you to provide the parameters as AppSettings references, e.g. through `IOptions<>` but declaring and locating those AppSettings is the responsibility of the calling project.
* Equally, if you have the literal values to hand, you could simply pass the `Func<>` as `(_) => "myConfigValue"`.

#### Config Parameters

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

### Tests

HMD tests use checked in versions of the allele data we fetch from WMDA. Originally they were directly copied from a full version of the WMDA data, but to speed up testing, alleles unused in any unit test have been removed from some of the files - as such they should not be considered to be valid representations of the WMDA data.

Any new alleles required in testing the HlaMetadata Dictionary should be added, and any no longer used can be removed.

### Extending the HMD with new Lookups

#### Background
Originally, the HMD was internal to the matching algorithm project, and the services were designed to lookup the HLA metadata needed to perform matching and scoring.
These services were later extracted to their own dedicated project, with a public external interface accessible to any Atlas project,
and the HMD has been extended with lookups required by other features, such as the match prediction algorithm.

In terms of design, this means that some services within the HMD remain coupled to matching and scoring, whereas the newer services are de-coupled to allow usage by any consumer.
There are cards in the Atlas backlog to clean up the HMD (ATLAS-394), but in the mean time, here is some guidance for how to add new lookups in the context of the existing design.

#### Design
Logic within the HMD can be split into the following categories:

##### WMDA Data Extraction
Retrieval and basic parsing of IMGT/HLA database nomenclature files (of a specified version) to produce the "WMDA dataset".

##### Data Generation
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

##### Data Retrieval

###### Metadata Lookup
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

###### HLA Conversion
HLA conversion relies on the same lookup services described above, but provides a subtly different API to consumers.
- The HLA converter is used when the consumer wishes to directly convert between two HLA typing categories, e.g., small g group to P group.
- Not every possible combination of HLA typing categories is currently supported; options are limited.

#### Examples of Previous Lookups
Adding a new lookup path to the HMD involves changing multiple files; the best way to ensure all necessary changes have been made is to emulate previous commits.

- Lookup that should handle any supported HLA category: search git history for `ATLAS-879` (lookup the equivalent small g groups for any support HLA category).
- HLA converter/lookup that handles one HLA typing category: search git history for `ATLAS-880` (convert small g group to its equivalent P group).
