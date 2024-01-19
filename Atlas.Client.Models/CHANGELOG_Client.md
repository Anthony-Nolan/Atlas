# Atlas.Client.Models

## Description
This package contains all client models utilised by the Atlas Public API to request searches and repeat searches, and to retrieve their results.

* `Search` - models used throughout search journey, e.g., `DonorType`
  * `Requests` - request definitions and their initiation responses
  * `Results` - search result (includes both matching and match prediction results) and notification
    * `Matching` - matching result per donor/locus, and notification to allow early retrieval of match results
    * `MatchPrediction` - match prediction results per donor/locus
    * `ResultSet` - search results for a set of donors
    * `LogFile` - log file for a completed search request (note: a completely separate feature to Application Insights logging)

## Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### 1.7.0
* Added new namespace, `Debug`, for models that are used in debug endpoints.
    * Moved following models to `Debug` namespace:
      * `DebugDonorResult` used in existing donor debug endpoints.
      * `PeekServiceBusMessagesRequest` used in message peeker service.
    * Added new `Debug` model, `DonorUpdateFailureInfo`.
* Moved existing `Alert` and `Notification` message models (and dependent classes) to new `SupportMessages` namespace.

### 1.6.0
* `Search.Results.Matching.PerLocus.LocusPositionScoreDetails` has been extended with a new field: `IsAntigenMatch`, which indicates whether the match grade for a position is an antigen match (`true`) or not (`false`).
* Added `MatchingStartTime` property to `ResultSet` model.
* Added models representing the search request log file that is written after a search completes.
* `Search.Results.MatchPrediction.MatchProbabilityPerLocusResponse.PositionalMatchCategories` are now overridable, in case they need to be re-orientated.
* Extended `Search.Results.LogFile.SearchLog` with a `SearchRequest` property, and added a new child model, `MatchingSearchLog`, that inherits from `SearchLog` but has info unique to the matching part of search.
* Added `Summary` string to `FailureInfo` model to replace obsolete `SearchResultsNotification.FailureMessage` property.

### 1.5.0
* `ResultSet` model has been extended with new properties, `MatchCriteriaDenominator` and `ScoringCriteriaDenominator`, which represent the match count denominator for matching results and scoring results, respectively.
* `MatchingResultsNotification` and `SearchResultsNotification` have both been extended with more failure information.
  * As part of this enhancement, the following properties have been deprecated:
    * `SearchResultsNotification.FailureMessage` has been superseded by a new property `SearchResultsNotification.FailureInfo`.
    * `MatchingResultsNotification.ValidationError` has been superseded by a new property `MatchingResultsNotification.FailureInfo`.
* The client models project was refactored to replace dependency on Atlas.Common with a new project, Atlas.Common.Public.Models, which only contains those models referenced by both the client and other components.
* Changelog .md file included as NuGet Package README.

### 1.4.2
* No change to client.

### 1.4.1
* No change to client.

### 1.4.0

* New `BatchScoringRequest` model added, using new `IdentifiedDonorHla` model
  * Used to allow standalone scoring requests for batches of multiple donors at a time. 
* `ScoringResult` model has two new properties: 
  * `TypedLociCount`
  * `MatchCategory`
  * These properties already existed on the scoring information of search results, and have been added to the scoring-only result for parity

### 1.3.0

#### Search Requests

* Added optional `DonorRegistryCodes` property to search request, to allow filtering of donor results to those from certain registry codes. 

#### Search Results

* Marked 'PatientFrequencySetNomenclatureVersion' and 'DonorFrequencySetNomenclatureVersion' as Obsolete and added 'PatientHaplotypeFrequencySet' and 'DonorHaplotypeFrequencySet' to replace them on the 'MatchProbabilityResponse'.
  Both HF sets contain the metadata (internal ID, RegistryCode, EthnicityCode, HlaNomenclatureVersion and PopulationId) of the frequency set.
* Added 'MismatchDirection' to 'LocusSearchResult' to indicate the directionality of a DPB1 non-permissive mismatch.
* Replaced `SearchedHla` in result sets with the full `SearchRequest`, to aid debugging/support work looking at result sets. 

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

#### 1.1.0
* Renamed `HlaNomenclatureVersion` to `MatchingAlgorithmHlaNomenclatureVersion` on both result set and notification models,
  now that Matching and Match Prediction are able to use two different HLA versions.

### 1.0.0
* First stable release of Atlas client.