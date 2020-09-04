# Elastic Plan, not Consumption Plan For Import Processes

## Status

Accepted.

## Context

[ADR 014](014-ServicePlans_FunctionsApps_Structure.md) describes a consumption plan being used for donor import and HF set import.

In practice, this introduces an unwanted 10 minute maximum timeout for import processes, which we would prefer to increase.

## Investigation

For donor import files, particularly large files can take longer than 10 minutes to process, especially under high load. As we don't want to enforce limitations
on the number of donors in a file, a larger timeout was necessary.

Haplotype Frequency set import usually runs within 10 minutes on a scaled up database, but to run on a cheaper S0 tier, particularly large HF sets will start to hit the 
10 minute time out. 

## Decision

Both donor and HF import functions apps will instead be deployed to the shared elastic plan used by match prediction orchestration.

## Consequences

- There should be no price implications, as a pre-existing plan is in use
- Under very high load, match prediction orchestration and import processes could fight for resources
    - In practice this is very unlikely, given the high horizontal scalability of the elastic plan (50 instances)
