# Full Data Refresh - Donor Imports

## Status

Accepted

## Context

No donor updates should be lost when performing the data refresh.

Consider an example where a data refresh job imports the full donor dataset, and then spends time pre-processing these donors.
Any updates to donors received in this window would be applied to the active database, but not the currently processing database.
When hot-swapped, all such updates would essentially be reverted.

## Decision

Ongoing Donor Import functions are disabled for the duration of the data refresh

## Consequences

Donor updates will take longer than usual to propagate during the data refresh.