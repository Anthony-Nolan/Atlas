# Title

## Status

Proposed.

## Context

Various representations of donor ids exist - we should be clear about which identifiers we support/expect for donors.

## Decision

Two types of donor identifiers will be used within Atlas.

(a) "External Donor Code"

This is a string (while AN and WMDA donors have numerical IDs, this may not be the case for other consumers of Atlas).
Only this ID should be returned from Atlas in e.g. search results.

(b) "Atlas Donor Id"

This is an integer, assigned on import to the Atlas donor store. 
This should remain an integer to ensure optimal indexing / storage in the matching component. 

## Consequences

- We remain decoupled from the exact format of external consumer's donor IDs
- Performance is not negatively impacted by using string ids throughout.
- Debugging may be slightly more painful in some cases as donor ids may need cross-referencing to external ids. 