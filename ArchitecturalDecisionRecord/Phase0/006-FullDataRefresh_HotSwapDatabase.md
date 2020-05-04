# Full Data Refresh - Database Hot Swap

## Status

Accepted

## Context

The full data refresh should not make the search algorithm inaccessible - it should still return correct search results, 
without degraded performance.

## Decision

Two SQL databases are created for matching donors - known as Transient A/B. 
A "persistent" SQL database is used for tracking which of the two is currently active.
When the refresh finishes, the persistent data can be updated to use the newly refreshed database, 
leading to all future searches running against the newly active database.

## Consequences

Data refresh will not affect availability or performance of the matching algorithm. 
Two databases must be provisioned and managed. 