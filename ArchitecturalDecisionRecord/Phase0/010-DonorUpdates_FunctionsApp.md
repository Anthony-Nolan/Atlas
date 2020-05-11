# Donor Updates - Functions App

## Status

Accepted.

## Context

As part of the data refresh, donor import functions are disabled.

Disabling a function involves setting an application setting - which triggers an app restart.

If the donor import function was hosted on the same functions app as the data refresh, this would cause an interruption in the job itself. 

## Decision

A separate functions application is deployed, for donor management functions only.

## Consequences

Multiple apps must be deployed to Azure. 
Independent caches are maintained for each functions app, leading to a slightly increased overall memory footprint.