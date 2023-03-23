Service for the match prediction capabilities of the Atlas Search Algorithm

## Projects

The solution is split across multiple projects:

- Atlas.MatchPrediction
  - Contains the business logic for the match prediction algorithm
- Atlas.MatchPrediction.Data
  - Data access layer - manages the data schema (via EF code first), and access of said data via Dapper
- Atlas.MatchPrediction.Functions
  - **WARNING** This functions app does not actually run match prediction in a search request - instead that is performed by `Atlas.Functions` in the durable functions layer
  - This functions app provides import functionality for HF sets
  - It also exposes a HTTP endpoint intended for manual debugging/support of the match prediction algorithm and its component stages - not intended for production use while running searches.
- Atlas.MatchPrediction.Test
  - Unit tests for the project
- Atlas.MatchPrediction.Test.Integration
  - Integration tests, including covering a real database layer

## Overview

The "Match Prediction Algorithm" is an additional processing stage for every patient/donor pair that is returned by the 
[Matching Algorithm](README_MatchingAlgorithm.md).

The output of the match prediction algorithm is a percentage likelihood of a patient/donor pair having a specific match count. 

This percentage is provided for 0, 1, and 2 mismatches across all loci, as well as 
per-locus values being provided for each of 0/1/2 mismatches.

The algorithm makes use of reference data known as "Haplotype Frequency Sets" (or HF Sets) to come to this conclusion


### Algorithmic Summary

A high level overview of the match prediction algorithm's logic is as follows: 

#### Haplotype Frequency Set Selection
  * For each patient and donor, a suitable HF set is selected
  * Sets are identified by a combination of the ethnicity and registry data for the donor/patient. If a specific set cannot be found 
  for their ethnicity/registry data, a less specific set will be used, ultimately defaulting to the "global" or default set.
  
#### Genotype Expansion
  * Both patient and donor genotypes must be expanded from _potentially ambiguous allele representations of unknown phase_
  to a collection of possible *diplotypes* (unambiguous typing of known phase).
  * This could be achieved naively by expanding to *all possible* diplotypes, but this would generate far too many possibilities to run calculations on
  * Instead, the diplotypes are calculated from the chosen HF set - all haplotypes that are permitted by the input HLA are selected, and
  then all combinations of permitted haplotypes are considered to give us a set of possible diplotypes

#### Frequency Identification
  * For each expanded set of diplotypes, a likelihood for the diplotype must be sourced
  * As the diplotypes were built from haplotypes from the chosen HF set, the likelihood of a diplotype can be easily calculated 
  by multiplying the likelihoods of the two haplotypes it consists of.

#### Match Calculation
  * For each patient/donor *pair of diplotypes*, we calculate the match count at each locus. 
  * Match counts are determined by comparing P Group values - identical P groups are considered a match
    * In the case of null expressing alleles (which belong to no P group), the P group of its paired allele is used for this calculation, in keeping with the logic used in the matching algorithm
    * In the case of HF sets typed at a non-P group resolution, the data must first be converted to P groups. Therefore the only typing resolutions
    permitted for HF set data (P Group, G group, g group) must all be convertable to exactly 1 (or 0) P groups.

#### Final Calculation
  * For each of the percentage results, the final result can be calculated by dividing the `sum of all patient donor pairs' likelihoods that meet the result's criteria`
    (e.g. 0 mismatches overall) by the `sum of all patient donor pairs' likelihoods`


## Match Prediction Requests
- Match prediction requests (outside of search) can be submitted to the http-triggered function within the Match prediction project.
  - The endpoint accepts a single patient along with a set of donors (at least one donor must be submitted).
  - The endpoint will return a unique request ID for each valid donor input in the batch, and will return validation errors for any invalid donor inputs, i.e, those missing required info.
  - The function forwards the batch request onto a dedicated service bus topic; in this way, potentially millions of requests can be made and queued on the topic for gradual processing.
- A second, servicebus-triggered function reads messages in batches off the topic, and runs the requests.
  - Results are uploaded to a subfolder of the match prediction results blob storage container (subfolder name: `match-prediction-requests`).
    - Each json result file is named after its corresponding match prediction request ID.
    - Note, the file does not contain a patient or donor ID; the consumer should map patient-donor IDs to request ID when initially submitting the request.
  - At this point, if any requests contain invalid properties, such invalid HLA, these will be indiviually caught and logged to Application Insights to allow users to correct them and re-submit.
    - Note: No alerts are sent out in such case; the user should manually monitor the logs, or use Application Insights monitoring.


## Match Prediction Algorithm (MPA) Settings

### Handling HLA metadata dictionary (HMD) errors caused by HLA versioning

There is a known issue that occurs when the matching and match prediction components are run on different versions of the HMD, specifically where the former may be ahead of the latter, and contain new alleles. Refer to [issue #637](https://github.com/Anthony-Nolan/Atlas/issues/637) for more details.

Until a more permanent fix has been implemented, the following setting has been added to the MPA to control whether HMD errors will be thrown during the "phenotype conversion" step of the algorithm: `SuppressCompressedPhenotypeConversionExceptions: [true/false]`.

It has been set to `true` for search requests handled by the `Atlas.Functions` app, and to `false` for match prediction requests run by the `Atlas.MatchPrediction.Functions` app. This is because the above issue only occurs when both matching and match prediction are run sequentially on the same patient-donor set (i.e., during search). When only match prediction is run, HMD exceptions may be due to genuinely invalid HLA being submitted in a request that should rightly cause the request to fail.

Note to developers: `SuppressCompressedPhenotypeConversionExceptions` has been added as an app-level setting, as for the immediate future, there are no plans to run match prediction requests from the top-level function. If this changes before a fix for issue #637 has been implemented, the scope of controlling suppression of conversion errors may need amending.