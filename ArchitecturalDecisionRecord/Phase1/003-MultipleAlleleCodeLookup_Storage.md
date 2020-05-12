# Multiple Allele Code - Storage

## Status

Accepted.

## Context

Atlas can no longer rely on the internal Anthony Nolan HLA service - instead it must keep track of MACs (sometimes known as NMDP Codes) itself.

AN's solution involves lazily maintaining a store of known [Locus/FirstField/MAC] composite keys to expanded allele values.

## Decision

MACs will be stored as provided by NMDP (Code, specific vs. generic flag, Expanded Alleles) - i.e. just keyed by MAC, not by Locus or 
first field.

MACs will be stored in Azure Table Storage.

## Consequences

- When expanding MAC-typed HLA, invalid locus/first-field/code combinations will no longer be caught at the time of lookup, 
instead relying on per-allele lookups rejecting any invalid expanded allele.

[Performance]
- No second call to the MAC store will be necessary on the event of a failed lookup
    - In the old system, a missing key did not mean a failed lookup, it may have been a combination that was not yet added to the lazily 
    populated MAC store. 
    - In the proposed new system, a failed lookup can be considered a failure immediately
    - This should have a positive impact on search/pre-processing performance
- Generic allele MACs will need to have the first field expanded on lookup
    - Generic MACs only contain information about the alleles' second fields, and are applicable to any first field. 
    At time of lookup, the given first field will need to be added to all expanded second fields before individual alleles can be 
    processed.
    - This is expected to have a minor negative impact on search/pre-processing performance
  
[Risk]  
- No manipulation / lazy caching of the raw MAC data means there will be less to develop and test during project development.