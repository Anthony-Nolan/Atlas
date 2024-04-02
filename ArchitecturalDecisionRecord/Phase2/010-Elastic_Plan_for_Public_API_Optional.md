# Separate Service Plan for Public API should be optional

## Status

Proposed.

## Context

The elastic service plan for the Public API app is only needed for installations that receive many requests and need high availability. This is really only applicable to production. It would save significant costs to allow apps to run on one plan in all other environments, by making the separate API service plan optional. 

## Decision

A new terraform release variable will be used to control the creation of the separate plan for the Public API app.
It will be set to `false` by default.

## Consequences

- Running costs of the algorithm will be significantly lower where only one plan is used.
- Due to the risk that search initiation may be affected when only one plan is in place, appropriate load testing should be performed before using this configuration in production.