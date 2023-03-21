# Maintenance

This README contains useful information for maintaining the ATLAS system after it has been integrated.

## Differential Donor Import
After the initial `full` load of donors into Atlas, the donor store can be kept in-sync with the external donor register via differential donor updates. These import files have the same [schema](/Schemas/DonorUpdateFileSchema.json) as `full` files, but the `updateMode` must be set to `diff`.

Further reading:
* [Donor Import README](/README_DonorImport.md) for more info on donor import - both, `full` and `diff`.

## Donor Checkers
Functions are available to check whether the donor info held in the Atlas donor store is as expected. The consumer can then generate donor updates (i.e., `diff` donor import files) where differences have been reported.

Further reading:
* [Donor Import README](/README_DonorImport.md#donor-checker-functions) for info on how to use the donor checker functions.

## Data Refresh Job
This job updates the HLA Metadata Dictionary (HMD) to the latest released IMGT/HLA nomenclature version, and re-processes donor HLA typings within the matching algorithm to this new HLA version. If configured to run automatically, it will run every time a new IMGT/HLA release is detected (usually, quarterly); else, it can be configured to run manually.

Further reading:
* [Integration README](/README_Integration.md#data-refresh) for information on how to run the job.
* [Support README](/README_Support.md#data-refresh) for how to manually trigger the job and what to do if it fails.
* [Configuration README](/README_Configuration.md) for various settings that control how the job is run and resource allocation.
* [Matching Algorithm README](/README_MatchingAlgorithm.md#pre-processing) for info about the data processing itself.

