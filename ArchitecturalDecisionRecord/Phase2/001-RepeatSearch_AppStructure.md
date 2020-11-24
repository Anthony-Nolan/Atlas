# Repeat Search App Structure

## Status

Accepted.

## Context

Atlas is currently stateless, but needs to be able to handle hundreds of "repeat searches", or "active searches", daily - which provide a differential of the results since 
a search was last run.

This could theoretically be achieved by running all searches again, but such an approach quickly becomes infeasible a large volume of searches, and with a high quantity of 
donors - both of which are expected. 

## Decision

- Atlas will not automatically run repeat searches, not have a concept of "open" searches.

- Atlas will store the state required to calculate a differential of search results - this will be stored in a **new schema of the shared Atlas SQL database**.
    - It will need to contain, at minimum:
        - The donor ids previously matched, to be able to work out which donors to mark as removed in the differential.
        - The number of repeats that have been run so far
        - the run date of the repeat
    
- Repeat search will be triggered via an HTTP endpoint of the PublicApi Atlas function, as with non-repeat searches. Similarly to regular searches, this app will merely queue 
the request for processing, and provide the consumer a way to identify results when they are published.

## Consequences

- Repeat search will be possible to run frequently, on large data sets, within a reasonable timeframe
- Atlas will store more state in SQL about result sets - the memory footprint of Atlas will therefore increase, and continue to increase over time