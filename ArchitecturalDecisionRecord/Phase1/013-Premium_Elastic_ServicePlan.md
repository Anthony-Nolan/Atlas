# Premium-Elastic Service Plan

## Status

Proposed.

## Context

On a consumption plan, ATLAS searches were far slower than desired. Regularly scaling down to 0 instances mean we spent most of the time warming caches.  

## Investigation

Search performance testing was performed on both a consumption plan, and a premium elastic service plan.

## Decision

Atlas top level functions app (which runs search durable functions, including the work of both algorithms) will be deployed to a premium elastic service plan.

## Consequences

- Search times become comparable or quicker than the equivalent in the NOVA algorithm, rather than noticeably slower
- Rapid scaling for large result sets is still possible
- A number of pre-warmed instances can be specified - the higher the number, the quicker a large search or high load will be, in exchange for a higher base running cost
- Running costs of the algorithm will be slightly higher in periods of zero load, as the functions app will not scale down to 0 in periods of no load. 