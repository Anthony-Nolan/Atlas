# Specifications

Atlas is a software application developed in-house by Anthony Nolan for the purposes of finding matching unrelated donors (MUDs) and cord blood units (CBUs) for haematopoietic stem cell transplantation.
After generating a match list, Atlas scores how well the HLA typing of each potential stem cell donor matches to the patient and the probability that the donor is an allele-level match.
Search coordinators use this calculated matching data in combination with other factors, such as donor age and availability, to select those donors that will lead to the best post-transplant outcomes for the given patient.

Below is a list of features and requirements for the Atlas product. This is not an exhaustive list, but it does cover the most critical features.

## Donor Search 

- Note, within the Atlas domain, the term "Donor" can refer to either MUDs and CBUs.

### HLA Typings

- Accepts minimum patient and donor HLA typing of HLA-A, B, and DRB1.
- Handles a variety of HLA typing resolutions:
  - Allele, including two-field truncations, e.g., `A*01:01`
  - G group (e.g., `A*01:01:01G`) and P group (e.g., `A*01:01P`)
  - Allele strings (e.g., `A*01:06/07`, `A*01:06/A*01:07`)
  - MACs (e.g., `A*01:AB`)
  - Serology (e.g., `A1`)
- Typings are interpreted and validated using the latest version of the [IMGT/HLA nomenclature](https://github.com/ANHIG/IMGTHLA).

### HLA Matching

- Offers three, four or five loci matching at HLA-A, -B, -DRB1, and/or -C, -DQB1.
- Allows control of the number and placement of mismatches.
- Matching for both MUDs and CBUs is at the "allele-level", where two typings will be deemed mismatched if they are not at least a P group match.
  - Also read the FAQ: [Why doesn't Atlas offer antigenic matching for CBU search?](https://github.com/Anthony-Nolan/Atlas/discussions/835)

### Donor Scoring

- Six loci scoring to assess match quality, including HLA-DPB1 permissive mismatches and antigen matching of allele mismatches.

### Match Prediction

- Calculates match probabilities using HLA haplotype frequency (HF) datasets.
  - Probability of 0, 1 or 2 mismatches per MUD/CBU, and per match locus.
  - Predicted match category per MUD/CBU, and per locus (`Exact`, `Potential` or `Mismatch`).
- HF sets can be encoded as G groups or "small g" groups (a.k.a. "ARS sets", or P groups that include non-expressing alleles)
- HF sets can be assigned to a particular ethnic group type and registry for targeted HF selection during match prediction.
  - Patient regional metadata is submitted via the search request and donor metadata during [donor import](#donor-import).
- One HF set can be marked as the fallback `global` set to be used when the subject has no regional metadata or when no HF set has been assigned to the subject's metadata values.

### Repeat Search

- Automatically stores previously run search requests and their match lists.
- Accepts requests to re-run (repeat) a specified search request, and reports the diff when compared to what has been previously found:
  - New matches
  - Previous matches that have been updated since the specified cut-off date
  - Previous matches that are no longer matching or have been removed from the donor store

## Donor Import

- Imports donor data via JSON file and persists it to a database.
- Files can be `Full` (when first loading the donor store, or when wanting to fully wipe over existing records) or `Diff` (for frequent updates).
- Checks stored donor data against `check` request files and reports any differences found.

## Performance

- Capable of running searches against 10s of millions of donors.
- Search requests are queued and multiple requests can be executed in parallel.
- Application and its components can be horizontally scaled to deal with varying levels of demand.
- Vertical scaling can be used to optimise runtime efficiency (cost vs performance).

## Support

- Uses logging, alerting, and notifications to enable debug and support.
- Architectured to enable the automatic retry of search requests and other workflows to provide resilience against intermittent failures.
- API-level validation used to enforce minimum requirements.
- Codebase is open to investigation by any tech support team, and in the case of priority 1 issues, hot fixes can be applied to a forked copy without having to wait on the code-owner.

## Testing & Validation

- Code review on every pull request. 
- Automated tests used throughout the solution: unit, integration and some BDD tests.
- Match prediction component has been validated using the WMDA consensus dataset ([external link to write up](https://drive.google.com/drive/folders/1vI1pgqwiqZCFaJlAfzq41v3loWH6SqP7)).
- User Acceptance Testing (UAT) and Clinical Validation completed for MUD search by the Anthony Nolan Search team.
  - UAT of remaining features is ongoing during late 2022/ early 2023, to be completed prior to Atlas going live in WMDA Search & Match service.