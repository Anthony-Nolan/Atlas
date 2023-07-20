# Changelog

All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.6.0

* Added new index to `Active` column of `HaplotypeFrequencySets` to optimise deletion of inactive sets.

## <= 1.5.0

Prior to v1.5 of Atlas, no Changelog was actively updated. A snapshot of the schema at this time has been documented here, and all future changes should be documented against the appropriate version.

### HaplotypeFrequencySet

One record for each HF set. Includes all sets ever imported, not just currently active ones.

* `Id`
* `RegistryCode` - string registry code. Must match registry data provided with donors
* `EthnicityCode` - string ethnicity code. Must match ethnicity data provided with donors
* `PopulationId` - not used by Atlas - useful for consumer/support identification of HF sets by registry/ethnicity code combined id
* `HlaNomenclatureVersion` - HLA nomenclature version used to generate this set
* `Active` - whether this set can be used for match prediction. Only one set should be active per population. Allows quick rollback to old sets if new ones are problematic
* `Name` - name for manual identification of sets / support
* `DateTimeAdded`

### HaplotypeFrequencies

Individual Frequencies within a set

* `Id`
* `Frequency` - how likely this haplotype is within its population
* HLA data - in format specified by TypingCategory
* `A`
* `B`
* `C`
* `DQB1`
* `DRB1`
* `SetId` - FK to HaplotypeFrequencySets
* `TypingCategory` - HLA typing category used for the HLA in this haplotype record. Supported values:
  * small g group
  * large G group
  * P group
