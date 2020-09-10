# Support

This README contains useful information for supporting the ATLAS system, and some troubleshooting steps for common problems.

## Notifications 

Two types of notifications are sent from ATLAS, and should be routed to appropriate communication channels as part of ATLAS setup (see the [integration README](README_Integration.md))

- Notifications

These contain potentially useful information, such as details on successful import processes.
 
- Alerts

These contain information that should be actioned - e.g. failures in import processes. 

We recommend routing these to different channels, so that alerts can always be acted upon.


### Haplotype Frequency Upload

#### Allele lookup error

The haplotype frequency import process involves validating and converting the provided hla data.

> Is lookup failing for the first allele in the file?

If the failed lookup is a very common allele e.g. `01:01:01G` - and is the first one in the uploaded file - it is likely that th **HLA Metadata Dictionary** has not been generated for
the given nomenclature version. Check the version used in teh file, and trigger a Metadata refresh.

// TODO: ATLAS-727: Improve error messaging in this scenario

> Is the allele valid, and a G-Group?

ATLAS only supports haplotype frequency data as G-Groups. This means that if you provide an allele that is valid, and represented by a G-Group (and was perhaps a G-Group of one in a 
previous nomenclature version) - the file will be rejected.


#### Rolling back an upload

Haplotype Frequency sets are soft-deleted as part of the import process. This means that in the case of an issue with the latest set for a registry/ethnicity, it is possible to 
very quickly roll back to a previous version.

This is not automated - it will require manually changing the SQL database. The active set in question will need to have its `Active` column set to 0, and the desired replacement set to `1`. 
These two steps must happen in order, as only one set can be active (per ethnicity/registry pair) at a time.

Alternatively, an older file can be re-uploaded, which will automatically become active if successful. This does not require database access, but does require access to the upload file
for the desired rollback, and will take longer than the manual SQL approach - especially for large sets (order of minutes).   


### Deleting old sets

As there is no hard delete in the upload process, no sets will ever be deleted during ongoing operation. 

It may be desirable to manually delete older sets, to free up database space and keep database operations quick.