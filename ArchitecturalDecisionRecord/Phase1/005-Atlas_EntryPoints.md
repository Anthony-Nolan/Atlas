# Atlas Entry Points

## Status

Proposed.

## Context

The Atlas system consists of several independently releasable components, but requires a single public interface for ease of integration. 

## Decision

An "Atlas" Azure Functions app will be deployed, containing all public entry points to the Atlas system, e.g.

- Search Initiation
- Manual "HlaMetadataDictionary" regeneration
- Scheduled function to regularly update MAC store 

## Consequences

- Provides one public API for Atlas that can be documented and shared with consumers
- Removes the need to add deployable apps for all Atlas components. 
e.g. MAC importer does not need an independently deployed functions app, just storage will suffice.
Some components will still need their own functions apps, so we can control scalability and configuration of such components independently. 