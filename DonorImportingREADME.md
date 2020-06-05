# Summary

Donor JSON Files are uploaded to BlobStorage, where they are picked up for processing by the Donor Import Function. Records are inserted directly into the master Donor Database.

* If the Donor File denotes a *change* to a Donor, then a Donor Update Message is put in the ServiceBusQueue to be processed into the Matching Donor Database.
* If the Donor File is marked as being part of a "full" upload, then no such Message is Enqueued, and it is expected that a full Donor Refresh will be manually triggered in the near future.

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
