# Repeat Search - Canonical Result Set

## Status

Accepted.

## Context

Atlas was designed to be a purely stateless search function. However, as a requirement of the repeat search functionality, it needs to be able to work out diffs for a given search request, to show which donors have been removed
from teh set i.e. were matching but no longer exist / no longer match.

## Decision

Atlas will continue to not store the bulk of search results (outside of the serialised data in blob storage consumed by Atlas' consumers.)

The Repeat Search component will store a "Canonical Result Set". This will represent the current up to date set of matching donors, calculated the last time a repeat search was requested.

This will be initially stored as the full result set from a non-repeat search. For all repeat searches for a given result set, the repeat search component will calculate: 

- Which donors should newly be added to the canonical set 
- Which donors should be removed from the canonical set
- Which donors should remain in the set

The reported output of repeat search will be the diff of the previous set, and the latest one.  

## Consequences

- By storing a canonical set rather than storing all reported diffs, performance will not be impacted when searches have a large number of repeats run

