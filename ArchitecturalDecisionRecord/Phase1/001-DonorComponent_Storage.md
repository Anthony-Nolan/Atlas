# Donor Component - Storage

## Status

Proposed.

## Context

Atlas will now need a master donor store, containing information that can be used for both matching and match prediction.
This store will need to import large quantities of donors / donor updates regularly. 

## Decision

Donors will be stored in an Azure SQL database.

## Consequences
