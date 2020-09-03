# Atlas Integration Guide

This README should cover the necessary steps to integrate ATLAS into another system, assuming it has been fully set up.

For a deployment guide, see [the relevant README](README_Deployment.md) 

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

TODO