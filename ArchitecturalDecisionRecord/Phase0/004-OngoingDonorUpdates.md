# Ongoing Donor Updates

## Status

Accepted.

## Context

A requirement of the matching algorithm is that any updates (additions, updates to details, removals) of donors should be reflected in search
results with 24 hours.

## Decision

- Azure Service Bus is used to track ongoing updates.
- The updates are batched, to ensure suitable performance with high update throughput.
- Donor HLA updates will not be applied to the SQL database unless the HLA is confirmed to have changed.
- The messages contain timestamps, and the most recently applied donor update per donor is tracked, to ensure updates cannot be applied out 
of sequence
- Donors are never deleted, only marked as inactive and filtered out of searches 
    - This is much more performant than deleting the donor and corresponding p-group rows from the large MatchingHla tables.

## Consequences

- Due to the batching of messages, if one donor in a batch causes an exception in processing (e.g. invalid HLA), then the entire batch
will be rejected. 
- Due to the update timestamps, failed messages can be safely replayed, and only updates more recent than the currently applied one will 
be applied. This prevents the data becoming stale due to out of sequence messages. 