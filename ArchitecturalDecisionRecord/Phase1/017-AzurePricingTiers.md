# Azure Pricing Tiers

## Status

Accepted.

## Context

We want to strike the right balance between performance and running costs for ATLAS, in addition to making the infrastructure that will need scaling up with donor pool size 
configurable, to avoid needing to fork the project to run on a much larger dataset.

## Investigation

Most testing was performed against an "Anthony Nolan" sized donor pool: ~2 million donors.
In addition, some testing was performed against a "WMDA" sized donor pool: ~30 million donors.

The two things we can change the pricing tier for are database sizes, and service plans. (there is a premium tier of service bus, but it is unlikely to noticeably affect performance) 
In this ADR we will consider each component of ATLAS independently, but first an overview of the available tiers.

### Azure Pricing Tier Investigation  

#### Service Plans

The majority of intensive Atlas work occurs on one of three **Elastic** service plans. 

The available tiers are EP1, EP2, EP3.

Each tier increases the amount of memory + cores available on each instance. 
Due to being a premium plan, at least one instance must always be provisioned per plan. 

Testing indicates that each tier increase roughly halves the time taken for algorithmic work (matching, match prediction) - which is in line with the number of cores doubling at each tier.

#### Databases

There are various types of database tier available: 

- Standard          = cheapest, provisioned database
- Premium           = more expensive, provisioned database
- VCores            = another option for more expensive, provisioned database
- Serverless        = a variant of the VCore based model, which will auto scale with load, and has the option to turn off entirely after a period of no usage.
- Hyperscale        = dynamically allocated storage. Similarly priced to basic VCore model. NOTE you cannot scale back down from a hyperscale database to any other tier.
- Business Critical = the most expensive VCore based model. 

ATLAS testing has been performed primarily on the Standard and VCore based models - for AN usage we have deemed other tiers to be too expensive, and unlikely to be necessary.

Generally testing has shown that if we hit the DTU limit of a tier and are throttled, performance is significantly impacted. 
Otherwise, scaling up the database does not tend to have a large enough effect to be worthwhile (with the exception of needing a premium tier when running 
the data refresh, which writes hundreds of millions of rows and relies on the improved IO of the premium tier.)

##### Serverless

Serverless may be a cheaper option were auto-sleep enabled. All databases except the dormant matching database are used during search requests, which we want to stay fairly snappy, and not add
several minutes of database cold-start time - so we have decided against using it on Atlas.

The dormant matching database could benefit from being on a serverless plan, as it would be off most of the year - but such a change is outside of the scope of phase 1 of Atlas and we 
won't focus on it now. The cost of a provisioned S0 database is £14 per month - so such a change would save a little less than that amount.

S4 is the first standard database tier that would be *more expensive* to provision than a serverless that did not auto-sleep (provided the VCore range was set to 0.5-4). If needing to scale up any database
to S4 or beyond permanently, Serverless may be a more cost effective option and would then warrant further investigation.  

## Decision

### Service Plans

- Public API

This service plan does not do any intensive work, and exists to isolate inbound requests from expensive algorithmic work. Can be as cheap as possible: EP1.

- Matching 

This was originally separated to enable independent scaling limitations for matching and match prediction. 
The cheapest option would be instead to rely on app setting based scale limitations (these are known to be not 100% reliable, though they have shown to be fairly
reliable when triggered by a service bus, and ignored when using HTTP.)

> Recommend rolling this into the core Atlas service plan if the above settings based approach handles concurrency limitation appropriately. 

If not, recommend making this configurable, with the default being EP1.

- Other

Runs match prediction, as well as donor/HF Set import processes. 

> Recommend making this configurable, with the default being EP1. 

*The easiest way to improve algorithm performance will be to scale up this plan - each tier upgrade will double algorithm performance.*   


### Databases

- Donor Import

Full donor import requires a significant amount of IO, and will run a lot faster on a larger database. This is expected to be a one-off process, so would recommend 
manually scaling up before running that - ideally to a Premium or VCore based tier.

For ongoing imports / search requests, S2 appears to be the minimum tier at which there is no DTU throttling. For high throughput of ongoing updates, this may need to be increased further.

> Recommend making both the SKU size (within the "Standard" tier), and memory limit, configurable - default to S2, 30GB

- Match Prediction

Match prediction database is queried for a lot of data as part of the MPA, and there is a medium amount of IO required for HF set import (hundreds of thousands of row insertions for large sets).

We have found that S2 is the recommended minimum tier at which there is no DTU throttling.

> Recommend making both the SKU size (within the "Standard" tier), and memory limit, configurable - default to S2 - 30GB

- Matching 

> Matching persistent database is tiny - it should never need more than an S0, so we will not make this configurable.

Matching transient databases are managed by the data refresh job. The tiers (for a hardcoded allowed list of SKUs) are already configurable, across the Standard/Premium tiers.

This has been extensively tested previously in the Nova integration, and additional work has been done investigating the refresh running size as part of the Atlas project.
Recommendations are S0 for dormant, P4 for refresh, and S3 for active - these have been determined as the most cost-effective tiers for the 2 million donor dataset. 

> Recommend also making the max size configurable, to allow for varying needs for donor pool sizes 

**Important note for larger datasets**

Not only will larger donor pools need larger databases to run searches efficiently, they will also be limited by the maximum sizes allowed for certain tiers. 

For the 30 million donor case, the fully pre-processed donor set took just under 1TB - for that dataset, S3 was still possible, but the dormant database could not be scaled to an S0.

As this was so close to 1TB, for the full WMDA dataset you would need more than 1TB to account for a growing global donor pool - at this point even S3 is not large enough. 

Within the current system (i.e. only standard/premium tiers supported), the minimum database that could house this data pool is a P11, which costs ~ £6500 to run. 
This would need to be maintained for the dormant database as well, leading to total running costs of £13,000 per month. 

At this point I would highly recommend some additional project work, to allow scaling to either a serverless or hyperscale database. 
(Remember that scaling to a hyperscale database is a one-way operation, so any changes to the codebase to scale to hyperscale should be optional, so smaller 
installations have the option not to use it.)

## Consequences

- This ADR is mostly documenting recommended service tiers, rather than making any actual changes
- Some additional scaling settings will be made configurable via terraform variables, allowing different instances of ATLAS to handle different sizes of donor pool without codebase changes.