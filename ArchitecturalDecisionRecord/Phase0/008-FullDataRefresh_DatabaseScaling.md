# Full Data Refresh - Database Scaling

## Status

Accepted

## Context

Different database pricing tiers are suitable for different tasks.

## Decision

Three sizes of database are configurable for the matching algorithm transient databases: 
- Active Size
- Dormant Size
- Refresh Size

The "P" tier of Azure SQL database pricing is recommended for the data refresh, as it has much better IO performance for bulk inserts.
The "S" tier was shown to be sufficient for day-to-day use of the search feature.
For most of the year the dormant database is not in use, and anything more than the cheapest database tier would be a waste of funds.

The recommended values for these tiers are: 
- Active Size: S4
- Dormant Size: S0
- Refresh Size: P1
 
 but as they are configurable, this can be tweaked according to usage needs.

Scaling between these sizes is managed automatically by the data refresh job - via HTTP requests to the Azure management API.

## Consequences

Databases pricing tiers will be used as appropriate for the different jobs they perform.
Attention must be paid during the data refresh, as an interrupted refresh can lead to a more expensive dormant database that does not get 
automatically downscaled.