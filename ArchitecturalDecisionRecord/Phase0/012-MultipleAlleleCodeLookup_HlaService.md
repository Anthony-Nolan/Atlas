# Multiple Allele Code Lookup - (Nova) Hla Service

## Status

Accepted.

## Context

The matching algorithm needs to have the ability to expand Multiple Allele Codes (henceforth referred to as MACs, colloquially known as
NMDP codes, after the registry by which they are managed)

This information is provided by NMDP, rather than WMDA (as with other HLA metadata, handled in the matching dictionary)

Anthony Nolan's existing systems already maintain a store of these codes, exposed via the "HLA Service".

MACs come in two forms - generic (representing a list of possible second fields for an allele, theoretically valid at any locus), and 
specific (representing a list of possible two field alleles - i.e. first and second field - theoretically valid at any locus). 

The existing Anthony Nolan system lazily adds records for each Locus/First Field/MAC as they are encountered - this information
is what is exposed by the HLA service, keyed by the composite [Locus/FirstField/MAC] - as such, any combinations that are initially 
unrecognised must be explicitly checked, and, if valid, added to the persistent store.   

## Decision

The Anthony Nolan HLA Service is used to provide MAC lookups. 

## Consequences

Any failed MAC lookups must be re-checked with the HLA service over HTTP, in case they are valid but have not yet been encountered.