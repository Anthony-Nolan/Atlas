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
  * The list of active sets is cached: see [The active set cache](#the-active-set-cache).
  
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

Six choices follow from that, and each is load-bearing rather than incidental:

1. **The projected pool is cached per frequency set, not rebuilt per subject.** Grouping a set's haplotypes by typing
   category depends only on the set, so it belongs to the set's cache entry (`FrequencySetCacheEntry.ProjectedPool`).
   `HaplotypeFrequencyCache.BuildEntryFromDatabase` builds it eagerly, on the thread that loads the set.
2. **The pool holds interned allele ids, not allele names.** The pool filter is the hottest loop in the expansion — it
   runs H times per subject and the large majority of tests fail. Comparing ids turns each test into an array read, and
   a `HaplotypeKey` is far smaller in memory than the equivalent name form.
3. **A frequency is resolved once per survivor, not once per genotype.** A genotype's likelihood is the product of its
   two haplotype frequencies, and a frequency is a pure function of `(set, haplotype, excluded loci)`. Resolving per
   genotype does O(S²) awaited cache lookups where O(S) suffice.
4. **A genotype is carried as two pool indices and an eight-byte name key, and nothing is built until truncation has
   chosen.** A `PhenotypeInfo<T>` is seven objects. Building one per candidate pair, when 2,000 of a million survive,
   is the bulk of the allocation. Both `GenotypePair` and `GenotypeNameKey` are eight bytes in a contiguous list.
5. **A genotype's name identity is two interned haplotype ids, not its name form.** The likelihood dictionary is keyed
   by `GenotypeNameKey`, which indexes one entry per *distinct* haplotype name form. There are only S of those, against
   up to a million kept pairs, so the name forms are interned once per survivor and the pairing loop keys on integers.
   This is the same equality as the name form, not an approximation of it — see the type's own remarks for why.
6. **Each kept genotype is handed over with its name form and its likelihood.** `ExpandedGenotypeTruncater` builds the
   name form once per *surviving* key, at the first point at which which-keys-survive is known, and returns
   `ImputedGenotype` triples. `GenotypeConverter` therefore neither re-derives a name form nor probes a
   phenotype-keyed dictionary for a likelihood the truncater already had in hand.

The same "resolve once, where the inputs are still in hand" reasoning applies twice more:

* **Typing categories.** The phenotype is converted only to the categories that will be read. A category the frequency
  set holds no haplotypes in cannot affect the result, and the unambiguous short circuit reads `SmallGGroup` alone.
  Today every frequency set in DEV holds `SmallGGroup` only, so most of that conversion work is dead — but this is read
  from the set rather than assumed, so a future GGroup or PGroup import still works.
* **P group conversion.** A P group is a pure function of `(locus, group name, typing category)` once both HLA
  nomenclature versions are fixed, and genotypes are pairs drawn from the survivor pool — so the same triple recurs
  across genotypes. `GenotypeConverter` resolves each distinct triple once and then reads a dictionary per position,
  rather than issuing a conversion per genotype position.

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
  index-aligned with `GenotypeNameKeys`; anything that appends to one without the other mis-pairs a genotype with
  another genotype's likelihood.
* **`ImputedGenotypes.SumOfLikelihoods` is summed over surviving name forms, not over genotypes.** Two genotypes that
  differ only in typing category share one name form and one likelihood, so summing `Genotypes` would double-count.
  The sum is also folded in descending-likelihood order, because `decimal` addition is order-sensitive and this number
  is what the whole prediction is normalised against.
* **`ImputedGenotypes.Genotypes` is ordered, and that order is the expansion's.** It is pairing order, hence survivor
  order, hence the projected pool's order. Downstream sums run over this sequence.

## HLA versioning
- There are two places in the algorithm where HLA typings have to be converted to a specific HLA category:
  - [Genotype Expansion](#genotype-expansion) (original typing category to HF set typing categories)
  - [Match Calculation](#match-calculation) (HF set categories to P groups)
- Atlas allows for HF sets to be encoded to a HLA nomenclature version that is older than the one used by the matching algorithm.
- The match prediction algorithm first tries to convert HLA typings using the HF set HLA version.
- If this first attempt fails (e.g., when an allele belongs to a subsequent nomenclature version), it will attempt to convert the typing using the matching algorithm HLA version (as long as it is different to the HF set version).
- If both attempts fail, there is a significant risk of the subject being deemed "unrepresented", depending on the point at which conversion fails and the overall typing resolution.

## The active set cache
Each frequency set selection reads the list of active sets: one set for each registry and ethnicity pair. The list is
kept in two caches:
- **In each process**, for `HaplotypeFrequencySetCache:ActiveSetCacheExpiryMinutes` (1 minute). The expiry is absolute:
  a process that reads the list all the time still reads it again each minute.
- **In the distributed cache** (Redis), which all processes share, for 24 hours. When no Redis is configured
  (`DistributedCache:ConnectionString` is empty), each process reads the list from SQL instead.

**After an import.** Before the import sends its success notification, it reads the new list from SQL and writes it to
the distributed cache. The importing process uses the new set at once. The other processes use it when their own copy
expires: within 1 minute of the notification. 30 seconds later, the import writes the list again. Another process can
read SQL before the import activates its sets, and write what it read to the distributed cache after the first write of
the import. The second write replaces that old list.

**When the distributed cache does not store the list.** Each write that it does not store is logged as a warning. The
import writes the list again every 30 seconds, up to 4 times. If the list is still not stored, the import sends a
medium-priority alert: other processes can then use the replaced sets for up to 24 hours. When the cache is available,
call the refresh endpoint (below).

**A change of the active sets with SQL** (for example, a rollback: see
[README_Support](README_Support.md#rolling-back-an-upload)), or a restore or a copy of the database (see
[README_Support](README_Support.md#after-a-restore-or-a-copy-of-the-match-prediction-database)), does not change the
distributed cache. After the change, call `POST /api/RefreshActiveHaplotypeFrequencySetCache` on the match prediction
functions app (function key needed). It writes the list twice, 30 seconds apart, as an import does, so it returns after
30 seconds or more. It returns `200` when the distributed cache stored the list, and `503` when it did not: then call it
again later.

**SQL reads are limited.** In each process, one read of the list runs at a time, and concurrent callers wait for it. So
when the distributed cache misses or cannot be reached, each process reads SQL at most once a minute. There is no limit
across processes: a lock in Redis cannot work while Redis is down, which is when the processes read SQL.

**Long operations.** A set can change while a donor import or a data refresh (stage 65) computes genotype sets. These
operations do not keep one list of active sets for their whole run: each batch looks up its sets once. Each stored
genotype set records its frequency set, and search does not use a stored set of a replaced frequency set
(`StaleFrequencySet`, see [When a donor is computed live](#when-a-donor-is-computed-live)). See also the stage 65
[failures](README_MatchingAlgorithm.md#failures).

## Precomputed donor genotype sets (search)
Donor genotype sets can be computed ahead of time and stored in the transient matching database. Today the differential
donor import writes them, for the donors it adds or updates (ATL-232). A Data Refresh stage that fills them for every
donor is planned (ATL-233); there is no separate backfill. Search also stores the donors it computes live (see below).
When search finds a usable stored set for a donor, it decodes that set and uses it for match counting. It does not run
imputation, truncation or the P group conversion for that donor (ATL-221). The patient's set is stored and reused too,
once per search (ATL-426, see [Precomputed patient genotype set](#precomputed-patient-genotype-set-search)).

### Switching it on and off
The kill-switch is a mode setting:
- `Atlas.Functions` (sequential Durable path): `MatchPrediction:Precompute:Mode`.
- `Atlas.MatchPrediction.Worker` (parallel path): `Precompute:Mode` (env var `Precompute__Mode`).

Set both to the same value.

| Mode | Search with `UsePrecomputedGenotypeSets` null | `true` | `false` |
|---|---|---|---|
| `ForceLive` (default) | live | live | live |
| `DefaultLive` | live | precomputed | live |
| `DefaultPrecomputed` | precomputed | precomputed | live |

- `ForceLive` is the hard off: it ignores the request, so it can stop the precomputed path for every search at once,
  without a deploy. It stays the setting until clinical sign-off.
- The value is resolved once for each donor batch, not when the search is submitted. So changing the mode also affects
  searches that are already running. Repeat search uses the same `SearchRequest` value.
- When the precomputed path is off for a search, nothing stored is read, trusted or written: every donor runs the
  complete live pipeline.
- The standalone match prediction endpoint (`Atlas.MatchPrediction.Functions`) always computes live. It has no access
  to the transient databases, and it does not send the logging event below.

### Which database is read
Matching records the data refresh record it ran against (`ResultSet.MatchingAlgorithmDataRefreshRecordId`, ATL-433).
Match prediction reads stored sets only from that record's database, and only while it is still the active one. If a
Data Refresh has swapped the active database since matching ran, the batch is computed live. The old (dormant)
database is never read or written: it is scaled down, can be paused, and is wiped by the next refresh.

### When a donor is computed live
A stored set is used only when it was computed from the typing the search has for the donor (the typing matching read)
and with the frequency set the search uses. The stored row follows the donor's current typing and codes, and a donor
import can change them after matching.

Each donor input (one per distinct phenotype and frequency set metadata) has one of these outcomes. The names are the
metric names of the logging event below.

| Outcome | Applies to | Meaning |
|---|---|---|
| `Precomputed` | donor | A stored set for the search's typing, allowed loci and frequency set was decoded and used. |
| `NoRow` | donor | No stored set for any of the donor's ids. For example: the donor was added or updated before pre-computation existed, or recently; or pre-computation failed for the donor during donor import. |
| `StaleFrequencySet` | donor | A stored set for the search's typing exists, but it was computed with a different haplotype frequency set from the one the search now uses (for example, a new frequency set was imported). Neither its payload nor its "unrepresented" flag is used. |
| `TypingChanged` | donor | The only stored sets were computed from a different typing: a donor import changed the donor after matching read it. |
| `DecodeFailed` | donor | A stored set exists but could not be decoded. |
| `UncoveredAllowedLoci` | batch | The search's loci are not one of the four precomputed combinations: `{A,B,C,DRB1,DQB1}`, `{A,B,C,DRB1}`, `{A,B,DRB1,DQB1}`, `{A,B,DRB1}`. |
| `ActiveDatabaseChanged` | batch | A Data Refresh swapped the active database after matching ran (see above). |
| `ActiveDatabaseUnknown` | batch | The search was matched before matching recorded its data refresh record. |
| `ReadFailed` | batch | Reading the stored sets failed. This is logged as an exception, and the batch is computed live rather than failed. |
| `PrecomputeDisabled` | batch | The precomputed path is off for the search (see the mode table). |

### Storing donors computed live
When a donor has `NoRow` or `StaleFrequencySet`, search stores the set it has just computed, so the next search can use
it. The stored set is the same as the pre-computation would store: the same genotype set service, frequency set,
nomenclature version and key. Nothing is stored for the other outcomes:
- `TypingChanged`: the donor's current typing is not the one computed here.
- `DecodeFailed`: the bad row has the same key as the live set, so a store would find it and change nothing. The next
  Data Refresh, which rebuilds the tables, repairs it.

The store is best effort: a failure is logged (event `Precomputed genotype set store failed`) and never fails the
search. It runs after the batch's results are uploaded, but the batch ends only when it returns, so it does delay the
end of the batch and of the search. Its lock timeout keeps that delay short. Guards:
- Nothing is written unless the database matching used is still the active one. This is checked again just before writing.
- A donor's set is written only while the donor's typing, registry code and ethnicity code in the database are still the
  ones the set was computed from. A donor import can change a donor between matching and match prediction.
- The write takes the table lock on `DonorSubjectGenotypeSets` before it reads `Donors`. The donor import deletes a
  donor's rows first in its transaction, before it writes `Donors`, so the two take their locks in the same order.
  Whichever goes first wins safely.
- The write runs with `DEADLOCK_PRIORITY LOW`, so if a deadlock still happens, the write is the victim, never the import.
  It also runs with a 5-second `LOCK_TIMEOUT`, so a wait behind an import or another batch's store ends in a skipped,
  logged store instead of a long delay.
- One case is not covered: when `OngoingDifferentialDonorUpdatesShouldBeFullyTransactional` is `false`, the import
  deletes a donor's rows and writes the donor's new typing as two separate commits. A write between them can store a
  set for the old typing. The import's own pre-computation, which runs straight after, then replaces it. Only if that
  also fails does the old set stay, until the next Data Refresh. Search does not use it either way: the typing check
  above reports `TypingChanged` for it.

### Logging
Each donor batch sends one Application Insights event, `Precomputed genotype sets used`.
- Properties: `SearchRequestId`, `AllowedLociKey`, `UsePrecomputedGenotypeSets` (the resolved value),
  `UsePrecomputedGenotypeSetsSource` (`Request`, or `FeatureFlag` when the mode decided), `PrecomputeMode`,
  `MatchingAlgorithmDataRefreshRecordId`, `PatientGenotypeSetSource` (see below).
- Metrics: `DonorCount` (donor inputs), `DonorIdCount` (donors), `PrecomputedCount`, one count for each outcome in the
  table above, and `StoredCount`, `StoreSkippedDonorChanged`, `StoreSkippedDatabaseChanged` (in donors).

Application Insights can sample these events under load (adaptive sampling is on by default in both hosts). A plain sum
then undercounts, so weight each event by `itemCount` when adding up a search's totals:

```kusto
customEvents
| where name == "Precomputed genotype sets used"
| where customDimensions.SearchRequestId == "<search request id>"
| summarize
    Batches = sum(itemCount),
    DonorCount = sum(todouble(customMeasurements.DonorCount) * itemCount),
    PrecomputedCount = sum(todouble(customMeasurements.PrecomputedCount) * itemCount),
    NoRow = sum(todouble(customMeasurements.NoRow) * itemCount)
```

## Precomputed patient genotype set (search)
Without this, each match prediction batch imputes the patient again, so a search with N batches imputes the same patient
N times. Instead, the patient's set is stored once per search, and each batch decodes it (ATL-426). It follows the same
mode, request override and database rules as the donor sets above.

### The warm step, once per search
Before the batches are uploaded, `Atlas.Functions` (`MatchPredictionInputBuilder`, for both the sequential and the
parallel path, and for repeat search) runs `PatientGenotypeSetWarmer`:
1. If matching found no donors, it stops: there are no batches to use the set. A repeat search often has no new
   donors. If the precomputed path is off for the search (the mode table above), it stops too.
2. It finds the patient's haplotype frequency set, with the same lookup the live path uses.
3. It builds the key `(typing key, frequency set id, allowed loci key)`. This is the same key a donor with the same typing
   and frequency set has, so the two share one stored row.
4. If a row with the key is stored, it uses it (`Hit`). Otherwise it computes the set exactly as a batch would, and
   stores it (`Created`).
5. It puts the key on the search's request, so every batch input carries it (`PatientGenotypeSetKey`). The parallel
   Worker reads it from the batch blob.

Only a `SubjectGenotypeSetValues` row is written; a patient never gets a `DonorSubjectGenotypeSets` row. The step is
best effort: if it fails, it logs the error, leaves the key empty, and the batches compute the patient live. It is safe
to repeat, for example when the activity is retried: the second run finds the row.

**Which database.** As for donors, the step reads and writes only the database of the data refresh record that matching
used, and only while it is still the active one. It checks again just before writing. If a swap happens after that
check, the row lands in the old database. It is still correct there (it was computed with that database's nomenclature
version), and the next Data Refresh wipes it. So no clean-up is needed. Patient rows, like all stored rows, are wiped
with the tables at the start of every Data Refresh.

### The batch read
Each batch reads the patient's row together with its donors' rows, under the same checks. It uses the stored set only
when all of these hold; otherwise it computes the patient live, as before. Batches never write the patient's set.

| `PatientGenotypeSetSource` | Meaning |
|---|---|
| `Precomputed` | The stored set was decoded and used. The patient was not imputed for this batch. |
| `NoKey` | The batch has no key: the warm step did not store the set (path off at the time, a database swap, or an error). |
| `NoRow` | No stored row has the key. |
| `StaleFrequencySet` | The batch now uses a different patient frequency set from the one in the key (a new set was imported after the warm step). |
| `DecodeFailed` | The stored row could not be decoded. |
| `UncoveredAllowedLoci`, `ActiveDatabaseChanged`, `ActiveDatabaseUnknown`, `ReadFailed`, `PrecomputeDisabled` | The batch-wide reasons in the donor table above. |

The batch decides the mode again, so changing the mode after the warm step still applies: in `ForceLive` the patient is
computed live even when the batch has a key.

### Logging
- The warm step sends one event per search, `Precomputed patient genotype set warmed`.
  - Properties: `SearchRequestId`, `Result` (`Hit`, `Created`, `Skipped` or `Failed`), `Reason` (why it was skipped:
    `NoDonors`, `PrecomputeDisabled`, `ActiveDatabaseChanged`, `ActiveDatabaseUnknown` or `UncoveredAllowedLoci`), `AllowedLociKey`,
    `HaplotypeFrequencySetId`, `PrecomputeMode`, `MatchingAlgorithmDataRefreshRecordId`, and the exception type and message
    when it failed.
  - Metric: `PatientGenotypeCount`, when the set was computed.
- Each batch's `Precomputed genotype sets used` event has the property `PatientGenotypeSetSource`.

## Checking stored genotype sets (debug endpoint)
SQL can show which stored rows exist, and their key, frequency set and "unrepresented" flag. It cannot read the stored
data, and it cannot make the typing key of a typing. The debug endpoint `POST debug/precompute/genotypeSets/compute`
on the Data Refresh functions app (`<ENV>-DATA-REFRESH-FUNCTION`, function key needed) does these parts. It can also
compare the stored rows with a live computation (the parity check, below). This gives the evidence that the stored sets
are the same as the live sets, before `MatchPrediction:Precompute:Mode` changes from `ForceLive`.

**Why the Data Refresh app.** One request can impute up to 80 sets (20 donors, 4 locus combinations), and each frequency
set that it loads stays in the app's cache for up to an hour. In the matching algorithm app, which runs searches, that
work would compete with searches for CPU and memory. The Data Refresh app runs no searches, it already has the imputation
pipeline and its settings, and it is the app that writes the stored sets (stage 65 and the queued donor updates of stage
80). So the endpoint can also be used in LIVE, where it only reads.

- **Input.** One typing (`HlaTyping`, for example a patient's), or up to 20 donors (`ExternalDonorCodes`). For donors,
  the typing and the registry and ethnicity codes are read from the matching database that `Database` names: the active
  one by default (see the dormant database, below).
- **Frequency set.** By default, it is chosen with the rules of search: the donor's codes, or `FrequencySetMetadata` for
  a typing (the global set when this is not given). `HaplotypeFrequencySetId` uses exactly that set, even when it is no
  longer active. `IsActive` in the response says whether search would choose the set now.
- **Computation.** Each locus combination of `AllowedLociKeys` (all four by default) is imputed live, with the engine
  that the pre-computation and search use, at the nomenclature version of the database that it reads. The response
  gives the typing key (`HlaTypingKey`, for SQL), `IsUnrepresented`, the genotype count, the sum of likelihoods and the
  size of the stored data. `IncludeGenotypes` also returns each genotype and its exact likelihood.
- **Save.** With `Save`, it stores the sets in the active database. It finds or creates the value rows, and reports each
  value row as `Created` or `AlreadyExisted`. Then it writes the link rows of each donor whose requested values are all
  stored, for the requested combinations only. It replaces the link rows that the donor has for these combinations. In
  two cases, the result is not what the pre-computation stores:
  - With `HaplotypeFrequencySetId`, the donor's link rows point at that set, also when search would not choose it. Use
    this to make a `StaleFrequencySet` case. Search then computes the donor live. If the search uses stored sets, it
    then stores the donor's set for the frequency set that it chose. So usually only the first such search reports
    `StaleFrequencySet`. To go back, save again without `HaplotypeFrequencySetId`.
  - With only some `AllowedLociKeys`, the donor keeps its link rows for the other combinations. Search checks each link
    row on its own typing key and frequency set, so this does not change the search results.

  Save does not check if a donor changed after the endpoint read it. If a donor import changes the donor's typing
  during the request, the link rows can point at the old typing. Search then computes the donor live (`TypingChanged`),
  and does not store it. To repair the rows, save again.

  Saving is allowed only when the app setting `Debug:AllowPrecomputeWrites` is `true` (Terraform variable
  `MATCHING_DEBUG_ALLOW_PRECOMPUTE_WRITES`). Turn it on in test environments only, because search reads these rows.
  When it is off, a request that saves gets `403` and changes nothing. A save never writes to the dormant database.
- **Errors.** An error of one donor or of one combination is given in its entry and does not fail the request. If the
  donors cannot be read, each donor code gives that error, with `Found` false. A request that is not valid gets `400`.
  If no data refresh has completed, or the request reads the dormant database and no refresh has written to it, the
  request gets `409`.
- **Time.** Each donor is imputed once for each combination, and the request must finish within the HTTP limit of 230
  seconds. If the request takes longer, the caller gets `502`, but the request continues in the app and can still save.
  To find out, look for the rows with SQL, or send the request again: a second save finds the rows and reports
  `AlreadyExisted`. The first request after a restart also loads the frequency set. Use few donors for ambiguous typings
  and with `IncludeGenotypes`, which can give a very large response. Each set that the endpoint uses stays in the app's
  frequency set cache, as in the other match prediction hosts.
- **During a data refresh.** The Data Refresh app has one instance, and stages 40 to 60 and stage 80 of a running refresh
  do heavy work in it. Do not call the endpoint during those stages: a large request can slow the refresh, or make the
  app restart, after which the refresh continues only when its lease and the watchdog allow. Calls during stage 65 are
  safe, because the precompute workers, not this app, do the imputations of that stage. To see which stage a refresh is
  in, see the stage completion columns of `DataRefreshHistory`.

### Parity check (`CompareWithStored`)
With `CompareWithStored`, each computed set is compared with its stored row, as search reads it. It only reads, so it
can also check the stored rows of LIVE. It cannot be used with `Save`, because a save changes the rows that it reads.
- **Which row.** For a donor: the value row that the donor's link row points to. For a typing, for example a patient's
  row that the warm step of search stored: the value row with the key of the live set. The key includes the frequency
  set, so a row of an older set is not found. To check such a row, give its `HaplotypeFrequencySetId`. For a donor,
  `HaplotypeFrequencySetId` changes only the set of the live computation, not the row that is read. So give it only to
  check a row that was stored with that set, for example after a save with an older set. If search has stored the row
  again with the set that it uses (see Save, above), send the request without it. For that row, a request with it gives
  `Mismatch`, and the first difference is `HaplotypeFrequencySet`, with the two set ids. If the row has another set than
  the set that search uses, the result is `StaleFrequencySet`.
- **What is compared.** The stored data is decoded and compared with the live set: the frequency set of the row,
  `IsUnrepresented`, the genotype count, the sum of likelihoods, and each genotype (its two resolutions and its
  likelihood). The order of the genotypes does not matter. The two positions of a locus are compared as they are: the
  engine is deterministic, so a swap is a difference. Likelihoods are compared by value, with no rounding and no
  tolerance: `1.10` and `1.1` are equal. As in search, an unrepresented row is the empty set.
- **Result.** Each set gets a `ParityCheck` with one `Result`. When more than one result applies, the result is the
  first in this table, which is the order of the checks of search.

| `Result` | Meaning |
|---|---|
| `Error` | The live computation failed, or the stored row could not be read. `Error` gives the text. |
| `NoStoredRow` | No row is stored. |
| `TypingChanged` | Donors only. The stored row was computed from another typing (`StoredHlaTypingKey`) than the donor's typing now (`HlaTypingKey`). Search does not use the row. |
| `StaleFrequencySet` | Donors only. The stored row has another frequency set (`StoredHaplotypeFrequencySetId`) than the set that search would use now (`SearchHaplotypeFrequencySetId`). Search computes the donor live. |
| `DecodeFailed` | The stored data cannot be decoded. `DecodeError` gives the text. |
| `Mismatch` | The stored set is not the same as the live set. |
| `Match` | The stored set is the same as the live set. |

- **Differences.** The comparison also runs for `TypingChanged` and `StaleFrequencySet`, to show the effect. When the
  row has another frequency set than the live set, the first difference is `HaplotypeFrequencySet`: the other
  differences then come from the two sets, not from the stored data.
  `DifferenceCount` gives the number of differences, and `Differences` gives the first `MaxDifferencesPerRow` of them
  (10 by default, from 1 to 1,000). Each difference gives the live and the stored value as text with all their digits,
  because a client that reads a JSON number as a double can round two different likelihoods to the same number.
- **Totals.** The response also gives `ParityCounts` (the number of sets with each result, also when it is 0),
  `DonorsNotFound`, and `SubjectErrors`: the subjects that failed as a whole, so that they have no sets to compare, for
  example because the donors could not be read. For evidence that the stored sets are correct, every count in
  `ParityCounts` except `Match` must be 0, and so must `DonorsNotFound` and `SubjectErrors`.
- **After a frequency set import.** Each app keeps the active frequency sets for
  `HaplotypeFrequencySetCache:ActiveSetCacheExpiryMinutes` (1 minute by default). So for that time, this app and search
  can still choose the old set (see [The active set cache](#the-active-set-cache)). Wait that long before you check for
  `StaleFrequencySet`.

### The dormant database (`Database`)
With `Database` set to `Dormant`, the endpoint reads the other matching database, which Data Refresh writes to: its
donors and its stored rows. So it can check the rows of stage 65 before the databases swap, for example while a refresh
is paused for a manual decision. During stage 65, the donors that are not done yet give `NoStoredRow`.
- **Version.** The sets are computed at the nomenclature version of the latest refresh that wrote to the database: an
  open refresh that is filling it, or the last refresh that filled it. A refresh writes to the database from the end of
  its data deletion stage. If no refresh has written to it, the request gets `409`.
- **A paused database.** A serverless dormant database can be paused. The first read starts it again, but that read
  fails (SQL error 40613). The donors, or the stored rows, then give that error. Call again after about a minute.
- **Read only.** The endpoint never writes to the dormant database, so `Dormant` cannot be used with `Save`.

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

The Worker also connects to the matching algorithm's databases, to read and store precomputed donor genotype sets (see
[Precomputed donor genotype sets](#precomputed-donor-genotype-sets-search)): `ConnectionStrings:MatchingPersistentSql`
(the persistent database, which holds the data refresh history) and `ConnectionStrings:MatchingTransientASql` /
`ConnectionStrings:MatchingTransientBSql` (the two transient databases).

### Cache warm-up for live imputation (ATL-476)

Live imputation looks up HLA Metadata Dictionary data in Table Storage. It is cached in memory, but a cold cache answers
each lookup with a single-row Table Storage read. A new worker replica starts cold, and the dictionary tables expire
from the cache once a day. In one DEV test, this meant about 240k single-row reads on the small g group tables alone.

So each search batch warms the dictionary tables match prediction reads before its patient and donors run
(`IMatchPredictionCacheWarmer`, on both the durable and the parallel path; `IHlaMetadataCacheControl.PreWarmMatchPredictionCaches`:
allele names, allele groups, small g groups, and the G group and small g group to P group lookups). It warms the
nomenclature version of each haplotype frequency set the batch's patient and donors use, and the search's matching
version. So a process loads only the versions it needs.

The scoring table is not warmed. Match prediction reads it only to find the G group of a null allele in a typing, and to
convert a typing to G or P groups for a frequency set that holds haplotypes at those groups. With small g group
frequency sets, only subjects with a null allele read it, and most processes never do. The first such lookup is a
single-row read that starts a load of the whole table in the background, as for any table that is not warmed. If
frequency sets at G or P group are imported, every subject reads the scoring table, and it should be added to the
warm-up.

MACs are not warmed. A DEV test warmed all of them (1.36 million): it took 74-100 s on each new replica, raised worker
memory by about 1.6 GB towards the 4 GiB limit, and made live imputation no faster. A MAC is looked up when it is first
seen, and then kept for the life of the process (`MacStore`).

After the first batch of a process the warm-up costs a few cache reads. A batch that has a stored genotype set for the patient
and for every donor skips it (`DonorGenotypeSetBatchContext.MayComputeLive`), because it imputes nothing live. The
warm-up is best effort: if it fails, a warning is logged (`Match prediction cache warm-up failed`) and lookups fall back
to single-row reads, as before. A warm-up that takes 1 s or more sends the event `Match prediction caches warmed`, with
`WarmUpMs` and the versions it warmed.

The MAC table client is one per process (`IMacTableClientProvider`), and the table is checked once. Before ATL-476,
the scoped `MacRepository` checked the table in its constructor, without `await`, so the parallel path (one scope per
donor) made one blocking Table Storage call per donor.

### `HaplotypeFrequencySetCache.AwaitConsolidatedFrequencyWarm`

Loading a haplotype frequency set also runs a missing-loci pre-consolidation, which is three passes over the whole set
(up to ~275,000 haplotypes). This setting decides whether the load **waits** for it.

* `false` (the default, and what every host did before the setting existed) — the load returns as soon as the set is in
  memory and the pre-consolidation finishes in the background. A caller arriving during the warm falls back to a direct
  per-haplotype scan, which is roughly a thousand times the cost of a warm read but affects only the few callers that
  lose the race. **Leave it false for anything serving a search**, where one request's latency is what matters.
* `true` — the load returns only once the consolidated collection is ready, so no caller can reach the fallback. **Set
  it true for a precompute**, where no single request's latency matters and the same set serves thousands of subjects.

The pre-consolidation runs either way, and there is exactly one writer of `FrequencySetCacheEntry.ConsolidatedFrequencies`
in both modes — the setting moves that work on to or off the critical path, nothing more. Note that a machine with spare
cores will show `true` as a wall-clock regression, because the warm no longer overlaps the first subjects.

### `HaplotypeFrequencySetCache.MaxCachedFrequencySets` / `HaplotypeFrequencySetCache.SetCacheExpiryMinutes`

Bound the size of `HaplotypeFrequencyCache`'s dedicated cache of per-set frequency data (`FrequencySetCacheEntry`),
which otherwise grows with every distinct haplotype frequency set (registry/ethnicity) touched within a rolling
window, since each stays fully resident, forever, per replica, until this bounds it. That's not literally unbounded —
it tops out at the number of active sets (currently 208, totalling roughly 3,200 MiB) — but it's still a process-wide
memory floor that grows independently of concurrency or request volume, and one no host should have to pay for sets
it isn't actively using.

* `MaxCachedFrequencySets` — the maximum number of distinct sets allowed to stay resident at once. Each set counts as
  one unit regardless of its own row count (a set can hold up to ~275,000 haplotypes). Enforced as a true
  least-recently-used eviction by `FrequencySetResidencyTracker`: once the limit is reached, admitting a new distinct
  set evicts the least-recently-used tracked set to make room, so the fresh one is always the one kept. This is
  driven explicitly rather than left to `MemoryCache`'s own `SizeLimit`, which doesn't evict on overflow — it rejects
  the new insert outright instead.
* `SetCacheExpiryMinutes` — the absolute TTL, in minutes, for a cached set.

Both are ops-tunable via config without a code change. Start conservative and observe via the
`Get All Frequencies from HF set - from SQL database` timed log line — a higher-than-expected rate of that line firing
means more distinct sets are active within `SetCacheExpiryMinutes` than `MaxCachedFrequencySets` allows, so sets are
churning out of the cache before they'd naturally expire.

**The match prediction worker has its own values: 30 sets, for 1,440 minutes** (Terraform
`MATCH_PREDICTION_WORKER_MAX_CACHED_FREQUENCY_SETS` / `MATCH_PREDICTION_WORKER_SET_CACHE_EXPIRY_MINUTES`; the other hosts
keep 10 sets and 60 minutes). A search load touches many sets: on DEV, 40 searches used about 80, and with 10 cached each
replica loaded each set about 3 times. Live imputation then spent 98 % of its time getting frequency data, and the
reloads drove frequent gen 2 garbage collections (ATL-476). The GC heap stayed under 1.6 GB of its 3 GB limit (75 % of
the 4 GiB container), which is the room the larger cache uses. Watch the memory of the `match-pred-ca` container app
(the `WorkingSetBytes` metric in Azure Monitor) before raising it further. Note that `Get All Frequencies from HF set - from SQL database` is a verbose trace and is
sampled in the worker; the consolidation trace logged with each load (`Calculating consolidated frequencies ...`) is
the one to count, weighted by `itemCount`.

Two code changes make each set load go further (ATL-476):

* `DonorInputBatcher` orders a search's donor inputs by frequency set metadata (registry, then ethnicity code) before
  it cuts them into batches, so a batch spans as few sets as possible and its donors run set by set.
* `CompressedPhenotypeExpander` asks for all of a donor's survivor frequencies in one call
  (`IHaplotypeFrequencyLookupService.GetFrequenciesForHla`), so the set is fetched from the cache once per donor rather
  than once per survivor.

