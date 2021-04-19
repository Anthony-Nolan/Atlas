# Summary

This readme exists to give context to the test resources located in this folder - any more general test and debug resources should go in the [Development Start Up Guide README](../README_DevelopmentStartUpGuide.md).

## Donor Import

- `DonorImportFileGenerator.js` can be used to generate donor import files for testing or debugging. To use it:
  * edit any of the config values in the `config` object. You will at least want to change `donorIdPrefix` as this is a primary key in the DB, and it will reject any duplicates.
  * Select Donor information from the Donor database and edit it as appropriate, before adding them to the `donorsHla` array.
    * (tip: When copied the information from SSMS it resulted in literal tabs instead of `\t`. You can fix this in VSCode by doing a find and replace, with regex enabled from `\t` -> `\\t`)
  * You can then run the code, either from your IDE (VSCode and Rider work fine for this), or from the command line with `node DonorImportFileGenerator.js`
