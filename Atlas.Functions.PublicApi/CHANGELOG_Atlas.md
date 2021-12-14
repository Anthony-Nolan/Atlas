# Changelog
Changelog for Atlas as a product: it will cover functional and algorithmic changes that affect Atlas as a whole.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning
Product version is represented by the version tag of the Functions.PublicApi project.
The project version will be appropriately incremented with each change to the product, and the nature of the change logged below.

## Versions

### 1.3.0

### 1.2.0

- Fixed scoring issue in which some DPB1 pairs were erroneously classified as a Non-Permissive Mismatch, when in reality they should be Permissive.
- `PermissiveMismatch` match grade has been removed and will no longer be assigned - see [Client Changelog](../Atlas.Client.Models/CHANGELOG_Client.md) for more details on this change. 

### 1.1.1

- All enum values will now be serialised to strings, to allow ease of parsing the serialised results files / http responses for external consumers, and for human-readability.

### 1.1.0

#### Changed
- Matching and Match Prediction algorithms are now able to run at different HLA nomenclature versions.
  - MPA will now use the HLA versions of the haplotype frequency sets referenced during match probability calculations.
  - Matching will continue to use the HLA version that was set at the time of the last successful data refresh.

### 1.0.1

#### Fixed
- Fix for bug that was preventing HLA metadata dictionary refresh to v3.44.0 of HLA nomenclature.

### 1.0.0

- First stable release of the Atlas product.