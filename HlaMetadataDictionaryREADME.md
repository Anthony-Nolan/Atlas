# Summary

The HLA Metadata Dictionary (HMD) Library is responsible for knowing how to interpret HLA descriptor strings in various forms; the "HLA Nomenclature".
It gets its original data primarily from files published by the WMDA, plus some data about MACs from NMDP.

On command (expected to be roughly quarterly), the HMD library will read the latest version of the HLA Nomenclature published by the WMDA (read from a mirror hosted by Anthony Nolan Bioinformatics). Having read it from raw data files from the WMDA, the HMD converts it into a the format that is of most use for itself, and slow-caches all of that formatted data in its own Azure CloudStorage Table.
When used, the HMD then caches the contents of those CloudTables in memory, although that initial caching is slightly slow. Cache-Warming methods are available to trigger that cache-to-memory action, if desired.

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

* `fetchAzureStorageConnectionString` (`string`)
  * A Connection string to the Azure CloudStorage account that the HMD should use to write its data when regenerating the Dictionary, and thus to read from when using the data.
* `fetchWmdaHlaNomenclatureFilesUri` (`string`)
  * The Base URI of the WMDA data source, to be used when regenerating data.
* `fetchHlaClientApiKey` (`string`)
  * Needed, in order to pass it to the MacDictionary, so that the MacDictionary can contact Nova.
* `fetchHlaClientBaseUrl` (`string`)
  * Needed, in order to pass it to the MacDictionary, so that the MacDictionary can contact Nova.
* `fetchApplicationInsightsSettings` (`ApplicationInsightsSettings`)
  * Details of the Logging Settings.
  * THe HMD assumes that it is being run in Azure with an ApplicationInsight system attached. It will identify that system automatically, but you must provide the default LoggingLevel. Likely "Info".

