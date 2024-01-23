# Donor Import

[Donor JSON Files](/Schemas/DonorUpdateFileSchema.json) are uploaded to BlobStorage, where they are picked up for processing by the Donor Import Function. Records are inserted directly into the master Donor Database.

## Full Mode

This process is maximally efficient, but does not propagate changes to the matching algorithm as standard - that will need to be manually triggered once all donor files 
have imported via [Data Refresh](/README_Integration.md#data-refresh).

This process is expected to be used as a one-off when installing ATLAS for the first time, to import all existing donors at the time of installation.

> Important: To allow full mode import, the Donor Import Functions app setting, `DonorImport:AllowFullModeImport` must be set to `true`. Once initial import has completed, it is strongly recommended that the setting be reverted to `false` to stop donor import files being submitted in Full mode, which would prevent donor updates from reaching the matching algorithm. When the setting is `false`, full mode updates will not be imported and the file will be reported as permenantly failed.

It should be able to handle millions of donors in a timeframe of hours. Performance can be improved further by manually scaling up the "-atlas" shared SQL database in Azure for
the duration of the import process.

Recommended donor file size is 10,000 - 200,000 donors per file.

## Differential Mode

This process is intended for ongoing incremental updates of donors - donors can be added, removed, or updated via this process. You can use "Upsert" change type - it will create the donor if it hasn't created yet or update the existing one. It could be handy if you don't track the state of the imported donors by the Atlas system. 

Differential donors will be automatically pre-processed for the matching algorithm, so there is no need to run a Data Refresh after triggering it.

It should be able to handle tens of thousands of donors in a timeframe of hours.

## Limitations

The donor import process does not fully guarantee ordering of the application of donor update files.
This means that ATLAS does not officially support processing of two donor files containing *references to the same donor* within a *ten minute window*.

Atlas *does* guarantee that **updates** will be applied in order of upload - so even if processed out of order, only the most recent update will be applied.

There are some cases where this is not enough - e.g. if a donor is created and then immediately updated - so we still recommend a lead time of ~10 minutes 
between uploading files containing the same donor.

A full description of all possible combinations of update files can be found later in this readme.

## File Validation

Contents of the donor import file are validated on import to check that all required fields have been submitted; the [schema file](/Schemas/DonorUpdateFileSchema.json) indicates which fields are required.
Validation failures are logged as custom events to Application Insights and saved to a database table `DonorImportFailures`.

`DonorImportFailures` table ([link to describing model](/Atlas.DonorImport.Data/Models/DonorImportFailures.cs)) contains donor information and error details.
In case of multiple errors for one donor, one row per each error is created.

Note: HLA typings themselves are not verified at this stage, as this would involve HLA metadata dictionary lookups, which would slow down the import process.
"Bad" HLA will be caught and reported at the point where the donor is added to the Matching Donor Database (either via Service Bus, or Data Refresh).

## File Transfer performance

### From Dev Machines to Azure Cloud

The Donor Import files for current WMDA data are 34.3 GB. When zipped with maximum compression that can be stored as a 1.5GB zip file. The raw files must be uploaded to the Azure Blob Storage - it can't (currently?) handle unzipping a file of Donor Files. On a 2Mbps (0.88 GBph) uplink, uploading 34GB of data will take ~39 hrs = 1.6 days.

The AzureStorageExplorer can handle uploading files to Blob Storage *in general*, but doesn't cope very well with trying to upload 100s of MB of data, let alone 10s of GB!

The main issue appears to be the file and HTTP concurrency. ASE attempts to uploads multiple chunks of all the files simultaneously, saturating the upload-link, and causing all of the chunks to fail to upload. There doesn't seem to be any great advantage to doing this, and it causes very high error rates, and low upload-speeds even when some of the uploads do actually succeed. You can prevent this from hapenning by controlling the settings in ASE.

In the ASE Settings > "Transfers", set "Network Concurrency" and "File Concurrency" to both be "1". i.e. it will only upload a single 8MB chunk of a single file, at a time.

*There is also a "Maximum Transfer Rate" option, which purports to constraint the network usage, but putting "0.5" into that option appeared to break everything, for one Dev?*

### From OneDrive To Dev Machines

If your source of the files is OneDrive, then acquiring the files in order to be able to upload them to Azure in the first place may also be troublesome.

Downloading multiple files from the OneDrive Web Interface appears to be appallingly slow (somehow their Zipping process is managing to make this worse!?) And the 34.3 GB of data is split across nearly 500 files, so downloading them one-at-a-time is infeasible.

The best approach is to install the OneDrive *App* (note you'll need to uninstall the older OneDrive *program* first), and "Synch" the folder of data to your machine. That should synch it as raw files. (alternatively get your data uploaded to OneDrive as a zip file in the first place!)

### Moving files amongst Azure Containers without going via your laptop

*Use the AzureCLI.* StorageExplorer is NOT good enough at this!
If you use `az storage blob copy start-batch` then you can instruct Azure to transfer 10s of GBs of data, directly between blob containers, in a 5s of minutes (~2.5GB/min observed). The command is asynchronous and the data doesn't attempt to transfer through your PC.
This can make "re-uploading" files to re-trigger Functions trivial.

After you run the command, it will list the files that will be copied, and all the files will appear in the destination container as 0 Byte files, over time the files will acquire sizes. Ordering by Size can show you the progress.

To transfer files between containers in 2 different storages accounts, you will need:

* SAS-token for the source account.
  * In the Azure Portal, create a SAS token with ALL the access (incl. all the resource types!) and enable HTTP access.
  * that will generate a token that starts with a question mark. `?sv=......`
  * due to a bug in `az storage blob copy start` you will need to delete that leading question mark!
* Connection String for the destination account.

Variables:

* `mydestinationstorageaccount`. e.g. `manualdonorfilestorage`
* `my-destination-container`. e.g. `wmda-sized`
* `mysourcestorageaccount` e.g. `testwmdaatlasstorage`
* `my-destination-container` e.g. `donors`
* `DefaultEndpointsProtocol=https;AccountName=mydestinationstorageaccount;AccountKey=Y2T*******w==;EndpointSuffix=core.windows.net`
  * This connection-string is for the SOURCE account, and should come from Azure Portal, naturally.
* `"sv=2019-10-10&ss=bfqt&srt=sco&sp=rwdlacupx&se=2020-06-15T23:40:27Z&st=2020-06-15T15:40:27Z&spr=https,http&sig=FiR69W*****0k%3D"`
  * This SAS-token is for the DESTINATION account, and should come from Azure Portal, naturally. Note the quotes, and the absent leading '?'

Final command:

    az storage blob copy start-batch --connection-string DefaultEndpointsProtocol=https;AccountName=mydestinationstorageaccount;AccountKey=Y2T*******w==;EndpointSuffix=core.windows.net --destination-container my-destination-container --source-account-name mysourcestorageaccount --source-container my-destination-container --source-sas "sv=2019-10-10&ss=bfqt&srt=sco&sp=rwdlacupx&se=2020-06-15T23:40:27Z&st=2020-06-15T15:40:27Z&spr=https,http&sig=FiR69W*******0k%3D"

Final command on multiple lines (for ease of reading):

    az storage blob copy start-batch
        --connection-string DefaultEndpointsProtocol=https;AccountName=mydestinationstorageaccount;AccountKey=Y2T*******w==;EndpointSuffix=core.windows.net
        --destination-container my-destination-container
        --source-account-name mysourcestorageaccount
        --source-container my-destination-container
        --source-sas "sv=2019-10-10&ss=bfqt&srt=sco&sp=rwdlacupx&se=2020-06-15T23:40:27Z&st=2020-06-15T15:40:27Z&spr=https,http&sig=FiR69W*****0k%3D"

`az storage blob copy start-batch --help` will show you various parameters available. Notably:

* `--dryrun`, which will list the files being moved
* `--pattern`, which allows a wild-card mask on which files to copy. e.g. `--pattern *foo*.bar`
* `--verbose`, which gives you an indication of what's happening in the 10-30 seconds whilst the command initiates the copies (otherwise it's just blank until all copies have been initiated.

### Donor Import file behaviour if out of order.

The three operations - create, update and delete - will cause problems if the files are processed in an incorrect order. The following table explains the outcomes we expect.

[^1]: Invalid donor opertation is logged, and processing continues for the rest of the file.

| File 1  | File 2 | Behaviour if in order | Behaviour if out of order     | Notes                                                                                                  |
|---------|--------|-----------------------|-------------------------------|--------------------------------------------------------------------------------------------------------|
| Create  | Create | AI logged[^1]         | AI logged[^1]                 | The first create will work correctly, then throw an error on the second import, not changing the data  |
| Create  | Update | Updated donor         | AI logged[^1], then out of date donor | This will not throw an error and the donor will be out of date.                        |
| Create  | Upsert | Updated donor         | AI logged[^1]                         | The first upsert operation will create donor correctly, then throw an error on the attempt to create the same donor, not changing the data |
| Create  | Delete | No change             | Out of date donor | Error attempting to delete non-existing donor, then will create donor in the database that shouldn't be there   |
| Create* | Delete | AI logged[^1], donor deleted  | Donor deleted & recreated     | *Where donor already existed. in the out-of-order case this donor should not be present, but is        |
| Update  | Create | update then AI logged[^1]     | AI logged[^1] then update             | This should be fine, the second create will be disregarded, support will be alerted                    |
| Update  | Update | 2nd update stands     | 1st update stands             | We guard against this. Updates will not be applied if the upload time is before the most recent update |
| Update  | Upsert | 2nd update stands     | 1st update stands             | We guard against this. Updates will not be applied if the upload time is before the most recent update |
| Update  | Delete | No donor              | No donor                      | No Donor in either case so this is fine                                                                |
| Upsert  | Create | Create then AI logged[^1]     | Create then discard changes   |                                                                                                |
| Upsert*  | Create | Update then AI logged[^1]     | AI logged[^1] then update             | *Where donor already existed                    |
| Upsert  | Update | 2nd update stands     | 1st update stands             | We guard against this. Updates will not be applied if the upload time is before the most recent update |
| Upsert  | Upsert | 2nd update stands     | 1st update stands             | We guard against this. Updates will not be applied if the upload time is before the most recent update |
| Upsert  | Delete | No donor              | Donor deleted & recreated     | In the out-of-order case this donor should not be present, but is                                                                |
| Delete  | Create | New donor             | AI logged[^1], then delete            |                                |
| Delete  | Update | No donor, AI logged[^1] | Donor updated and deleted, no error            | In this and the case below this is fine as should be deleted                                           |
| Delete  | Upsert | New donor       | Donor updated and not deleted, no error            | In this and the case below this is fine as should be deleted                                           |
| Delete  | Delete | No donor, no error       | No donor, no error            |                                                                                                        |

### Import Results
Both successful and failed import send result message to `donor-import-results` topic.

## Donor Checker Functions

### Check donor presence in Atlas store

* Upload json input file to `donor-id-checker/requests` subfolder in `donors` blob container 
  * Json file must contain donor pool, donor type and a string array of donors IDs ([link to request model](/Atlas.DonorImport.FileSchema.Models/DonorChecker/DonorIdCheckerRequest.cs))

  E.g.,
	```json
	{
		"donPool": "donor-pool",
		"donorType": "donor-type"
		"donors": [
			"record-id-1",
			...
			"record-id-N"
		]
	}
	```
* If absent or orphaned donors are detected, a results file listing the two sets of ids is uploaded to `donor-id-checker/results` subfolder in `donors` blob container with filename `original filename + timestamp`
  * [Results model](/Atlas.DonorImport.FileSchema.Models/DonorChecker/DonorIdCheckerResults.cs)
* `donor-id-checker-results` service bus topic recieves success check messages with filename and result count (i.e., total number of absent donors)
* `alerts` topic recieves messages if handled exceptions are thrown

### Compare donor fields in set with Atlas store

* Upload json file to `donor-info-checker/requests` subfolder in `donors` blob container 
  * Json file format is same as for `donor-import` ([link to schema](/Schemas/DonorUpdateFileSchema.json); [link to model](/Atlas.DonorImport.FileSchema.Models/DonorImportFileSchema.cs))
  * Important: `updateMode` must be set to `check`
* If donors differences are found or if donors are absent, a results file listing the record ids of such donors is uploaded to `donor-info-checker/results` subfolder in `donors` blob container with filename `original filename + timestamp`
  * [Results model](/Atlas.DonorImport.FileSchema.Models/DonorChecker/DonorCheckerResults.cs)
* `donor-info-checker-results` topic recieves success check messages with filename and result count (i.e., total number of existing donors with differences plus absent donors)
* `alerts` topic recieves messages if handled exceptions are thrown

### Common Client Models
Models used by both checker functions:
* [Success Notification model](/Atlas.DonorImport.FileSchema.Models/DonorChecker/DonorCheckerMessage.cs)


