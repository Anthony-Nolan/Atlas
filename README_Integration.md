# Atlas Integration Guide

This README should cover the necessary steps to integrate ATLAS into another system, assuming it has been fully set up.

For a deployment guide, see [the relevant README](README_Deployment.md).

## Notifications 

Several of the import process steps of ATLAS have some built in notification functionality. Two service bus topics will be created by terraform: 
`notifications` and `alerts`. Subscriptions can be added to these channels and consumed as appropriate for the installation. 

Azure logic apps can easily be set up to forward the notifications appropriately - e.g. to Slack, Microsoft Teams, etc...   


## Data Import

Before searches can be run, several sets of data must be imported into the system.

Some reference data is imported from third party services, such as NMDP and WMDA.
Other data must be provided by the owner of the Atlas installation - donor information, and haplotype frequency data.   



### Multiple Allele Codes

These will be automatically imported nightly, from source data [hosted by NMDP](https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip)

An import can also be manually triggered, if e.g. you don't want to wait until the next day to start using an ATLAS installation.

> The function `ManuallyImportMacs` should be called, within the `ATLAS-FUNCTIONS` functions app.



### HLA Metadata

HLA "metadata" refers to HLA naming conventions drawn from international HLA nomenclature, [hosted here](https://raw.githubusercontent.com/ANHIG/IMGTHLA/)

HLA metadata will be automatically imported for the latest stable version at the time of a matching algorithm data refresh (see below)

To enforce a recreation (e.g. in the case of a schema change), or to import an older version, manual endpoints can be hit.   

> The function `RefreshHlaMetadataDictionaryToSpecificVersion` should be called, within the `ATLAS-MATCHING-ALGORITHM-FUNCTIONS` functions app.



### Haplotype Frequency Sets

Haplotype frequency sets should be provided in the [specified JSON schema](Schemas/HFSetSchema.json).

They should be uploaded to the `haplotype-frequency-import` container within Azure Blob Storage. 
Anything uploaded here will be automatically processed, stored in the SQL database, and activated if all validation passes.

On success, a notification will be sent, and on failure, an alert.

Donors/patients will use an appropriate set based on the ethnicity/registry codes provided - such codes must be identical strings to those used in HF set import to be used.

A "global" set should be uploaded with `null` ethnicity and registry information - when no appropriate set is found for a patient/donor, this global default will be used.



### Donor Import

There are two supported donor import processes. Both use the [following JSON schema](Schemas/DonorUpdateFileSchema.json), and will be automatically 
triggered when any files are uploaded to the `donors` container in Azure Blob Storage.

In both cases, we expect performance to be better with several smaller files, than with larger ones.

#### Full Import

This process is maximally efficient, but does not propagate changes to the matching algorithm as standard - that will need to be manually triggered once all donor files 
have imported - see the "Data Refresh" section below.

This process is expected to be used as a one-off when installing ATLAS for the first time, to import all existing donors at the time of installation.

It should be able to handle millions of donors in a timeframe of hours. Performance can be improved further by manually scaling up the "-atlas-donors" SQL database in Azure for
the duration of the import process.

Recommended donor file size is 10,000 - 200,000 donors per file.

#### Differential Import

This process is intended for ongoing incremental updates of donors - donors can be added, removed, or updated via this process. 

Differential donors will be automatically pre-processed for the matching algorithm, so there is no need to run a Data Refresh after triggering it.

It should be able to handle tens of thousands of donors in a timeframe of hours.

*NOTE*

Due to the use of EventGrid to trigger the import process, ordering of donor file processing cannot be guaranteed. There are code-measures in place to prevent donor *updates*
being applied out of order - but there are some edge cases that may still cause errors in very rare cases - e.g. if a donor is created and then updated in two separate files, 
which are uploaded within a very short timeframe of one another. Such edge cases are described in detail in [the donor import readme](README_DonorImport.md) - they are considered
extremely unlikely, and alerts will be sent if the data is ever detected as being out of sync. 

If ordering of donor updates is even 100% guaranteed, we recommend replacing the file-based ongoing donor import process with a purely service bus based one, and controlling 
the order of updates before they make it to ATLAS.  



### Data Refresh

The "data refresh" is a manually triggered job in the matching algorithm. 

> It can be triggered via the `RunDataRefreshManual` function in the `ATLAS-MATCHING-ALGORITHM-FUNCTIONS` function app

The data refresh performs several operations: 

(a) Updates the Hla Metadata Dictionary to the latest version, if necessary. 
(b) Replaces all donors in the matching algorithm with the contents of the `Donor Import` component's donor database
(c) Performs pre-processing on all donors that is a pre-requisite to running searches 

The donor replacement is performed on a secondary matching algorithm database, which is only activated for search on completion - so running this process will not 
affect running searches. 

This process is expected to be run: 
- When first installing an ATLAS installation, after a "Full" donor import has finished
- Every three months, when new HLA nomenclature is published.

The process is expected to take several hours - and will increase with both the number of donors, and the ambiguity of their HLA. 
For Anthony Nolan's dataset (~2 million donors), the process takes 3-4 hours.

 