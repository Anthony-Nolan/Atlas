# Matching Algorithm - Search Orchestration

## Status

Accepted.

## Context

Searches can take a long time - they should be asynchronous for a pleasant user experience when initiating searches.

## Decision

Searches are triggered by a synchronous call to a HTTP triggered function - this assigns a unique ID to the search and posts a message
to a search requests queue on Azure Service Bus.

When the search is complete, the "Claim Check" pattern is used for search results - the results are stored in Azure Blob Storage, and 
a message posted to a topic on Azure Service Bus. This message contains summary information about the search, and the identifier 
generated on initiation. This identifier is used as a location in Azure Blob Storage for consumers of the results notification
to download the search results.

## Consequences

Searches run asynchronously, and can be queued if all matching agents are busy.
Claim check search results are not deleted by the Atlas system - the consumer is responsible for deleting the search results once consumed.