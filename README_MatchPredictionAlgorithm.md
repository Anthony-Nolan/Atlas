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


## Algorithmic Summary

A high level overview of the match prediction algorithm's logic is as follows: 

### Haplotype Frequency Set Selection
  * For each patient and donor, a suitable HF set is selected
  * Sets are identified by a combination of the ethnicity and registry data for the donor/patient. If a specific set cannot be found 
  for their ethnicity/registry data, a less specific set will be used, ultimately defaulting to the "global" or default set.
  
### Genotype Expansion
  * Both patient and donor genotypes must be expanded from _potentially ambiguous allele representations of unknown phase_
  to a collection of possible *diplotypes* (unambiguous typing of known phase).
  * This could be achieved naively by expanding to *all possible* diplotypes, but this would generate far too many possibilities to run calculations on
  * Instead, the diplotypes are calculated from the chosen HF set - all haplotypes that are permitted by the input HLA are selected, and
  then all combinations of permitted haplotypes are considered to give us a set of possible diplotypes

### Frequency Identification
  * For each expanded set of diplotypes, a likelihood for the diplotype must be sourced
  * As the diplotypes were built from haplotypes from the chosen HF set, the likelihood of a diplotype can be easily calculated 
  by multiplying the likelihoods of the two haplotypes it consists of.

### Match Calculation
  * For each patient/donor *pair of diplotypes*, we calculate the match count at each locus. 
  * Match counts are determined by comparing P Group values - identical P groups are considered a match
    * In the case of null expressing alleles (which belong to no P group), the P group of its paired allele is used for this calculation, in keeping with the logic used in the matching algorithm
    * In the case of HF sets typed at a non-P group resolution, the data must first be converted to P groups. Therefore the only typing resolutions
    permitted for HF set data (P Group, G group, g group) must all be convertable to exactly 1 (or 0) P groups.

### Final Calculation
  * For each of the percentage results, the final result can be calculated by dividing the `sum of all patient donor pairs' likelihoods that meet the result's criteria`
    (e.g. 0 mismatches overall) by the `sum of all patient donor pairs' likelihoods`

## Imputation: how it is shaped, and what must not change

Genotype expansion ("imputation") is the most expensive part of match prediction, and the part most often changed for
performance. This section records the shape it has and the properties any change to it must keep. The code carries the
same rules as short comments; this is where the reasoning behind them lives.

### Why it is shaped this way

The cost of expanding one subject is driven by two numbers: **H**, the number of haplotypes in the frequency set (up to
~275,000), and **S**, the number of those that survive the filter for that subject (tens on average, but thousands in
the tail). Pairing is O(S²), so a subject in the tail can produce over a million candidate genotypes, of which
truncation keeps 2,000 (see [ADR Phase2/006](ArchitecturalDecisionRecord/Phase2/006-MatchPredictionAmbiguousGenotypeApproximation.md)).

Four choices follow from that, and each is load-bearing rather than incidental:

1. **The projected pool is cached per frequency set, not rebuilt per subject.** Grouping a set's haplotypes by typing
   category depends only on the set, so it belongs to the set's cache entry (`FrequencySetCacheEntry.ProjectedPool`).
   `HaplotypeFrequencyCache.BuildEntryFromDatabase` builds it eagerly, on the thread that loads the set.
2. **The pool holds interned allele ids, not allele names.** The pool filter is the hottest loop in the expansion — it
   runs H times per subject and the large majority of tests fail. Comparing ids turns each test into an array read, and
   a `HaplotypeKey` is far smaller in memory than the equivalent name form.
3. **A frequency is resolved once per survivor, not once per genotype.** A genotype's likelihood is the product of its
   two haplotype frequencies, and a frequency is a pure function of `(set, haplotype, excluded loci)`. Resolving per
   genotype does O(S²) awaited cache lookups where O(S) suffice.
4. **A genotype is carried as two pool indices and built only if truncation keeps it.** A `PhenotypeInfo<T>` is seven
   objects. Building one per candidate pair, when 2,000 of a million survive, is the bulk of the allocation.

The same reasoning applies to typing categories: the phenotype is converted only to the categories that will be read.
A category the frequency set holds no haplotypes in cannot affect the result, and the unambiguous short circuit reads
`SmallGGroup` alone. Today every frequency set in DEV holds `SmallGGroup` only, so most of that conversion work is
dead — but this is read from the set rather than assumed, so a future GGroup or PGroup import still works.

### Invariants

These are clinically significant. Breaking one changes search results without failing an obvious test, so each is
pinned by `ImputationEquivalenceTests`, `ExpandedGenotypeTruncaterTests`, `PairRepresentationMaskTests` or
`PoolFilterAlleleIdTests`.

* **Enumeration order is part of the output.** The projected pool's order sets the survivor order, which sets the
  pairing order, which sets insertion order — and insertion order is the tie-break the truncater applies when two
  genotypes have equal likelihood. Which genotypes a capped subject keeps is a clinical output, so any reordering
  anywhere in that chain needs HLA-expert sign-off, not just a passing build. In particular, do not rebuild the pool
  from the SQL rows: project it from the frozen dictionary, which is what fixes the order.
* **Truncation selects by `(likelihood descending, insertion order ascending)`.** A plain bounded heap does not do
  this; the tie-break is explicit in `ExpandedGenotypeTruncater.MostLikelyFirst` and must stay so.
* **Interned allele ids never leave `FrequencySetCacheEntry`.** They are meaningful only against that entry's
  `Interner`, and a second entry for the same set — built after eviction or expiry — has a different id space. Never
  pass an id to anything that re-enters the cache, such as `GetFrequencyForHla`. Survivors are resolved back to names
  before they leave the pool filter.
* **A survivor's frequency is not its own stored frequency.** On any key with excluded loci — the majority of a set's
  rows — a survivor is nulled at those loci and therefore stands for a *group* of stored haplotypes whose frequencies
  are summed. Always ask `IHaplotypeFrequencyService.GetFrequencyForHla`; never carry a value off the pool.
* **The likelihood multiplication order is fixed:** position 1, then position 2, then the homozygosity correction.
  `decimal` multiplication carries scale, and these likelihoods are compared for exact equality.
* **`ExpandedGenotypes.GenotypePairs` is a list, not a set, and cannot lose an entry to de-duplication.** It is kept
  index-aligned with `GenotypeHlaNames`; anything that appends to one without the other mis-pairs a genotype with
  another genotype's likelihood.

## HLA versioning
- There are two places in the algorithm where HLA typings have to be converted to a specific HLA category:
  - [Genotype Expansion](#genotype-expansion) (original typing category to HF set typing categories)
  - [Match Calculation](#match-calculation) (HF set categories to P groups)
- Atlas allows for HF sets to be encoded to a HLA nomenclature version that is older than the one used by the matching algorithm.
- The match prediction algorithm first tries to convert HLA typings using the HF set HLA version.
- If this first attempt fails (e.g., when an allele belongs to a subsequent nomenclature version), it will attempt to convert the typing using the matching algorithm HLA version (as long as it is different to the HF set version).
- If both attempts fail, there is a significant risk of the subject being deemed "unrepresented", depending on the point at which conversion fails and the overall typing resolution.

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

## Worker Project Configuration

`Atlas.MatchPrediction.Worker` is a .NET Generic Host Worker Service (no Azure Functions dependency).
Settings follow the same convention as other non-Functions projects: all values live in `appsettings.json`, with secrets marked `"override-this"`.

Override secrets locally using **User Secrets**

The following settings require real values to run locally:

```json 
{
  "ApplicationInsights": {
    "InstrumentationKey": "override-this"
  },
  "MessagingServiceBus": {
    "ConnectionString": "override-this"
  },
  "NotificationsServiceBus": {
    "ConnectionString": "override-this"
  }
}
```
All other settings have safe defaults for local development (Azurite for storage, local SQL Server for the database).

