# Changelog
All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versions

### 1.3.0
- Marked 'PatientFrequencySetNomenclatureVersion' and 'DonorFrequencySetNomenclatureVersion' as Obsolete and added 'PatientHaplotypeFrequencySet' and 'DonorHaplotypeFrequencySet' to replace them on the 'MatchProbabilityResponse'.
  Both HF sets contain the metadata (internal ID, RegistryCode, EthnicityCode, HlaNomenclatureVersion and PopulationId) of the frequency set.
- Added 'MismatchDirection' to 'LocusSearchResult' to indicate the directionality of a DPB1 non-permissive mismatch.

### 1.2.0

* `MatchGrade` enum:
    * `PermissiveMismatch` value removed.
        * The match grade is a value that is calculated for allele pairs (rather than loci pairs), and as such search results will have two match grades per locus.
        For Dpb1, the permissive mismatch grade can only be calculated by considering the whole locus - so it does not make sense to assign a grade of `PermissiveMismatch`
        to an individual allele at a locus.
        * The `PermissiveMismatch` match category will still be available in the `LocusMatchCategory`, at a per locus level (though will only ever be assigned at the DPB1 locus),
        and in the `MatchCategory` enum for the overall consolidated value for a donor (i.e. when the only mismatches are permissive at DPB1)
        * Loci with a `PermissiveMismatch` category will still be assigned match grades - but within the grade, all mismatches will be called `Mismatch` - to know if the 
        mismatch at a locus is permissive overall, the match category **must** be used instead. 

### 1.1.1

### 1.1.0

#### Changed
- Renamed `HlaNomenclatureVersion` to `MatchingAlgorithmHlaNomenclatureVersion` on both result set and notification models,
  now that Matching and Match Prediction are able to use two different HLA versions.

### 1.0.0

- First stable release of Atlas client.