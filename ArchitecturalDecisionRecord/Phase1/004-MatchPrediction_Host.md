# Match Prediction

## Status

Accepted.

## Context

Atlas must support match prediction on all search results.

## Decision

Azure Functions will be used to run match prediction. 
Match prediction is expected to be very horizontally scalable - as each donor/patient pair can be independently calculated, 
so an auto-scaling consumption plan is expected to be useful in providing such scalability.

## Consequences

- Match prediction should be easily scalable via a Azure Functions Consumption Plan
- We will need to be wary of any expensive cold start behaviour that will be run each time the function scales out.