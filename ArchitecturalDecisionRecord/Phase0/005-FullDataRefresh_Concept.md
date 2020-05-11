# Full Data Refresh - Concept

## Status

Accepted.

## Context

HLA nomenclature is updated every 3 months. The matching algorithm must reflect these changes as soon as possible.


## Decision

A full re-import of all donors will be performed every three months.
No attempt at working out which donors are affected by the changes to HLA nomenclature will be made.

## Consequences

- The risk of bugs in the refresh process is drastically reduced - attempting to work out which donors are affected by nomenclature updates
would be a very complex process
- Any donors marked as inactive in ongoing donor updates will be removed as part of this process, so there is no need to clean them up 
outside this context
- The full refresh takes a long time - on the AN dataset the full process tends to take around 12 hours.