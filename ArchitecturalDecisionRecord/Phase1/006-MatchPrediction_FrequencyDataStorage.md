# Match Prediction - Haplotype Frequency Set Data Storage

## Status

Proposed.

## Context

Haplotype frequency sets must be stored and queried as part of match prediction.
Frequency sets will be available generically, registry-specific, or for a registry/ethnic-group combination. 

## Decision

A single Azure SQL database will be used for all (active and deprecated) frequency datasets.

## Consequences

- Multiple data sources for different frequency sets have been considered, for scalability and pricing
    - A single database approach risks introducing a single bottleneck to an otherwise horizontally scalable system
    - A single database has the same cost implications as multiple SQL databases in an elastic pool
    - Development of the algorithm will be significantly sped up without the need to dynamically create/migrate containers for
    each dataset
    
- If we require investigation into another storage approach, we can do so once the algorithm logic has been implemented and system tests 
cover any potential regressions   