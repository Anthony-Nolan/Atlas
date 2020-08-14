# General

The Multiple Allele Code Dictionary class collects up to date HLA information from an external source and stores it in an azure table storage account, creating a cache which can be accessed by consumers.

 ## Configuration
 
 During DI setup, you should call `ServiceConfiguration.RegisterMacDictionary(...)`.
 
 That method has several parameters declaring what the MAC Dictionary needs to know / have access to. Beyond those things explicitly declared by the parameters, the MAC Dictionary does not require or assume any other context or settings.
 
 The Parameters are all of the form: `Func<IServiceProvider, T>`. They will be passed to the service registration code, so that the values in question can be fetched at the point that the types need to be instantiated.
 
 * This allows you to provide the parameters as AppSettings references, e.g. through `IOptions<>` but declaring and locating those AppSettings is the responsibility of the calling project.
 * Equally, if you have the literal values to hand, you could simply pass the `Func<>` as `(_) => "myConfigValue"`.
 
 ### Config Parameters
 
 * `fetchMacDownloadSettings` (`MacDownloadSettings`)
   * `ConnectionString`: (`string`) The connection string to the azure table storage account where you wish to store MACs
   * `TableName`: (`string`) The name of the table in the above account to store this data
   * `MacSourceUrl`: (`string`) The URL for which to download multiple allele codes. This is expected to be a zip file containing exactly one file which contains the MACs in a plain text format.
 * `fetchApplicationInsightsSettings` (`ApplicationInsightsSettings`)
   * Details of the Logging Settings.
   * The MAC Dictionary assumes that it is being run in Azure with an ApplicationInsight system attached. It will identify that system automatically, but you must provide the default LoggingLevel. Likely "Info".
 
## Testing

There are a number of options for testing the MAC Dictionary and its relation to the rest of ATLAS, depending on the type of testing required.

### File Backed Mac Repository

For integration tests of other parts of ATLAS there is a `FileBackedMacDictionaryRepository`, located in `Atlas.HlaMetaDataDictionary.Test.IntegrationTests`
This uses a csv generated from azure storage with a similar structure. It is intended that such integration tests will use the entire MacDictionary with just the repository level mocked out.

To add new MACs to this file there are two options: manually or using azure storage explorer's GUI. The latter is better suited for adding large amounts of MACs.
* To manually add them, simply add a new row to the CSV file. THe format for a MAC is:
`02:01/03:01,Edm.String,3,DEF,2020-06-22T15:16:17.104Z,false,Edm.Boolean`
In order these are: 
    * `HLA`: you should change this to the HLA you want the MAC to represent.
    * `Typing for HLA`: Do not change this.
    * `PartitionKey`: This should be the length of the MAC code. So for `ABC` it would be `3`, for `ABCDEF` use `6`.
    * `Mac`: The MAC itself
    * `timestamp`: This is unused, so you can re-use this.
    * `isGeneric`: If it is a generic MAC, this should be set to true, otherwise, false.
    * `typing for isGeneric`: Again, do not change.
    
* To add using the explorer GUI, create a new local table, import the existing.csv file and add new entries. Azure storage explorer automatically adds the correct timestamp. You can then export the data into a new CSV file. **Important**: you must remove the first row from the exported CSV, since it contains the header names

### Mocking the entire MAC Dictionary

For unit tests of other parts of ATLAS, it is possibly to simply mock out the entire MACDictionary to return expected results.

### AzureStorage

For validation tests we want our data to be as close as possible to the real thing, so we use azure table storage. A full MAC Import should be run on this to ensure it contains the full MAC Data.
If you are using the same storage account for integration and validation tests, be sure to not use the TestAtlasMultipleAlleleCodes table, as it is used for import integration tests, so does not reliably contain test data.

### TestAtlasMultipleAlleleCodes

There is an azure table storage table created to test the import functionality of the MAC Dictionary in integration tests. Important data must not be kept here as it will be deleted while running integration tests.
