# Matching Algorithm Phases

## Status

Accepted.

## Context

While the core logic of the matching algorithm was agreed upon by the Anthony Nolan search team, implementation details of that 
algorithm can vary. 

## Decision

A three phase matching structure was implemented, with the aim of avoiding full scans of the matching hla tables (processed p-groups
per donor, per locus) at the loci with more information. (e.g. Locus A has a lot more entries than any of the other partitions, as AN's
donors tend to have more ambiguous typings at this locus.

Phase 1 finds all donors that fulfil the matching criteria at a subset of loci, ideally avoiding locus A.
Phase 2 uses the results of Phase 1 to pre-filter the remaining loci, finding all donors that match criteria at all loci.
Phase 3 performs additional filtering on donor type and registry. 

## Consequences

We believe this phase based approach will perform worse for searches that return a large number of donors in phase 1.
(In some searches, very permissive searches have failed due to too many donors being returned in phase 1)
As such, this approach may not scale as well as others to a larger donor set.