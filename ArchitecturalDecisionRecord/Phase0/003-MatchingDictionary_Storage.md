# Matching Dictionary Storage

## Status

Accepted.

## Context

HLA nomenclature data is regularly imported from a WMDA provided data-source and pre-processed for efficient access.

## Decision

Azure Table Storage is used to store this HLA data. 
A new table is created for each:
- type of information to be lookup up
- HLA nomenclature version used as the source data
- invocation of the regeneration of this data

A reference table contains a pointer to the correct table to use for each type of data and HLA nomenclature version 

## Consequences

Storing separate tables for different HLA nomenclature allows different HLA nomenclatures to be used at once.
No tables are automatically cleaned up, so the number of stored tables will only ever increase.