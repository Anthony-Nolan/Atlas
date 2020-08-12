# Durable Functions - Search Orchestration

## Status

Accepted.

## Context

We would like to horizontally scale MPA requests for a given search result set, following a "fan-out/fan-in" pattern.

## Investigation

Reading on recommended ways to implement this pattern within the Azure functions ecosystem was preformed: 
https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-cloud-backup?tabs=csharp demonstrates 
durable functions as a recommended approach for this pattern.

## Decision

We will use Azure Durable Functions for search orchestration - running the matching and match prediction algorithms.

## Consequences

- Both Infrastructure and code are much easier to implement (and less prone to error) for the fan-in/fan-out MPA
 pattern, as durable functions encapsulates this architecture for us
- All activities called from an orchestrator function must be on the same functions app - meaning we may lose the ability
to scale matching/match prediction independently of one another (at least without further changes)
- Activities called from an orchestrator function only guarantee they can be run "at least once" - so we risk performing 
extra unnecessary computation