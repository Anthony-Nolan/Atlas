# Match Prediction

## Status

Accepted.

## Context

The match prediction algorithm needs to be able to choose an appropriate haplotype frequency set based on donor ethnicity and registry.

## Decision

These properties will be stored as strings in both the frequency datasets, and the imported donor information. 
(As distinct from an enum or other list of pre-defined allowed values.)
For context, the JSON schema for WMDA donor import treats ethnicity as an enum, as do many AN systems. 

As the Atlas system only uses this information for cross-referencing haplotype frequency sets, it does not feel necessary to 
have it understand the meaning of the values.

## Consequences

The client of Atlas is responsible for ensuring that the ethnicity and registry values provided in donor import files and 
haplotype frequency sets are matching. 

No validation will be performed by Atlas on the values of these properties.

New ethnicities and registries will require no code changes to introduce to an Atlas installation.