# Summary

This readme exists to give context to the test resources located in this folder - any more general test and debug resources should go in the top level README.

## Matching Algorithm

- InitialRefreshData.sql

Normally Donor data would be inserted by a FullDataRefresh, which would create records in the Persistent DB recording the completion of a Refresh.
Because we're hacking that data into the transient DB, we need to simulate that record. This script does that.
Note that this row does NOT reflect the latest version WMDA HLA Nomenclature, as mentioned above; it is the version that the source data was generated on.

-ZeroResultsSearch.json

This is a validly formatted request, but doesn't have any matching donors in our initial Test Data.
However, it also doesn't trigger any NMDP lookups, so it's a useful test to check whether *other* aspects of the search are functioning.

-EightResultsSearch.json

This is a search that should actually return results! Created by taking a perfect copy of one of the first donor records.

## Donor Import

- `DonorImportFileGenerator.js` can be used to generate donor import files for testing or debugging. To use it:
  * edit any of the config values in the `config` object. You will at least want to change `donorIdPrefix` as this is a primary key in the DB, and it will reject any duplicates.
  * Select Donor information from the Donor database and edit it as appropriate, before adding them to the `donorsHla` array.
    * (tip: When I copied the information from SSMS it resulted in literal tabs instead of `\t`. You can fix this in VSCode by doing a find and replace, with regex enabled from `\t` -> `\\t`)
  * You can then run the code, either from your IDE (VSCode and Rider work fine for this), or from the command line with `node DonorImportFileGenerator.js`
