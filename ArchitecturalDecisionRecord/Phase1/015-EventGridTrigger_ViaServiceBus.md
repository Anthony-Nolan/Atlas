# EventGrid Triggers - via ServiceBus

## Status

Accepted.

## Context

Under high load for event-grid triggered import functions, we have seen concurrency issues with our database.

We have also found that event grid is very highly concurrent, and cannot be easily controlled - http (via webhooks) triggered functions appear
to bypass scaling limits under high load, and there is no known buffering in place for the events. 

## Investigation

Tested instead hooking the event grid subscription to a service bus topic, and confirmed that this gives us finer control over concurrency, and allows queuing in periods
of high load.

## Decision

Instead of triggering import functions via webhooks directly from event grid, we will instead write the event grid notification to a service bus, to allow data buffering.

## Consequences

- Import processes can handle high load gracefully
- Some complexity is removed from the deployment process, as webjob set up is no longer necessary as a second terraform script
- Very few code changes are required - terraform handles piping event grid to service bus very easily, and the implementations of functions themselves
remains identical, with only the trigger attributes changing
- Allows for an audit subscription on the service bus topic, for support purposes 