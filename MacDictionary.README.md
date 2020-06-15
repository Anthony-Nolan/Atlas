# General

The Multiple Allele Code Dictionary class collects up to date HLA information from an external source and stores it in an azure table storage account, creating a cache which can be accessed by consumers.

 ## Configuration
 
 During DI setup, you should call `ServiceConfiguration.RegisterMacDictionary(...)`.
 
 That method has several parameters declaring what the MAC Dictionary needs to know / have access to. Beyond those things explicitly declared by the parameters, the MAC Dictionary does not require or assume any other context or settings.
 
 The Parameters are all of the form: `Func<IServiceProvider, T>`. They will be passed to the service registration code, so that the values in question can be fetched at the point that the types need to be instantiated.
 
 * This allows you to provide the parameters as AppSettings references, e.g. through `IOptions<>` but declaring and locating those AppSettings is the responsibility of the calling project.
 * Equally, if you have the literal values to hand, you could simply pass the `Func<>` as `(_) => "myConfigValue"`.
 
 ### Config Parameters
 
 * `fetchMacImportSettings` (`MacImportSettings`)
   * `ConnectionString`: (`string`) The connection string to the azure table storage account where you wish to store MACs
   * `TableName`: (`string`) The name of the table in the above account to store this data
   * `MacSourceUrl`: (`string`) The URL for which to download multiple allele codes. This is expected to be a zip file containing exactly one file which contains the MACs in a plain text format.
 * `fetchApplicationInsightsSettings` (`ApplicationInsightsSettings`)
   * Details of the Logging Settings.
   * The MAC Dictionary assumes that it is being run in Azure with an ApplicationInsight system attached. It will identify that system automatically, but you must provide the default LoggingLevel. Likely "Info".
 
