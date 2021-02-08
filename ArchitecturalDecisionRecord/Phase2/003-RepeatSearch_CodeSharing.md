# Repeat Search - Code Sharing

## Status

Accepted.

## Context

As described in [ADR 001](001-RepeatSearch_AppStructure.md), repeat search will be an independent process triggered by consumers. 

However both algorithms run will be nearly identical (or in the case of match prediction, actually identical)

## Decision

- Independent pipelines will be set up in Azure for original searches / repeat searches (i.e. distinct message queues, results storage)
- Repeat search results will be stored in folders keyed by original search request id
- Code will be shared as much as possible between the two pipelines, aiming to be distinct only in entry-points
    - Results models will be shared, with an optional repeat id
    - In order to share as much match prediction orchestration code as possible, the pipeline that results data is sent to will be determined by the existence of a repeat search id 

## Consequences

- Code is effectively shared between original / repeat search, easing maintainability of shared updates
- Independent data pipelines allow for extension of the repeat search feature without affecting regular search

- The shared code means that it will be easier to introduce bugs where results data enters the wrong pipeline 