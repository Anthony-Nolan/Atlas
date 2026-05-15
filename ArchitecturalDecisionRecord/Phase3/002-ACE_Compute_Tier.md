# ACA Compute Tier Selection for Donor Scoring Workload

## Status

Proposed

## Context

Two Azure Container Apps (ACAs) are deployed within a shared Azure Container Apps Environment (ACE): one executing a **donor matching algorithm**, the other a **match prediction algorithm**. Both process donor batches from a Service Bus queue. KEDA scales replicas based on Service Bus queue length, with burst peaks reaching hundreds of replicas.

The environment is provisioned in **Consumption-only mode**. A decision is required on whether to retain the Consumption plan or migrate to a Dedicated plan under a Workload Profiles v2-enabled ACE.

Candidate options evaluated:

- **Consumption plan** — serverless, per-second billing, scale-to-zero capable; current environment is sufficient as-is
- **Dedicated plan (D-series)** — reserved node pool, per-node billing, fixed capacity; requires migration to a Workload Profiles v2 ACE

## Investigation

**Cold start impact assessed:** Cold start on ACA Consumption is typically 15–60+ seconds depending on image size and initialisation complexity. However, since scaling is triggered by Service Bus queue depth rather than inbound HTTP requests, messages remain durably queued while new replicas initialise. Cold start adds latency only to the first batch processed after a scale-from-zero event and does not affect overall throughput or user-facing latency.

**Free tier assessed:** The Consumption free tier (180,000 vCPU-seconds, 360,000 GiB-seconds per subscription per month) is exhausted within minutes at hundreds of replicas. Cost planning assumes full pay-as-you-go rates.

**Cost model compared:**

| Factor | Consumption | Dedicated (D4, 3-node min) |
|---|---|---|
| Billing unit | Per replica-second (active/idle) | Per node instance (flat) |
| Scale to zero | Yes | No |
| Estimated baseline cost | Pay only during processing | ~$700+/month regardless of load |
| Break-even | >~18 active hours/day | Steady, high-throughput workloads |

Given a mixed scaling pattern with moderate baseline and overnight idle periods, the Dedicated plan's flat node cost would be incurred during low-utilisation windows with no corresponding benefit.

## Decision

Retain both container apps — donor matching and match prediction — on the **Consumption plan** within the existing ACE. No environment migration is required.

Replica resource allocation is set to **2 vCPU / 4 GiB** per replica.

## Consequences

**Easier:**
- Cost is directly proportional to processing volume — no idle node charges during overnight or low-activity periods
- No node capacity planning required; the platform manages infrastructure scaling transparently
- No environment migration needed — existing ACE continues to serve both apps without reprovisioning
- Cold start impact can be optionally mitigated by setting `minReplicas: 1` on both apps, keeping a single warm replica at negligible idle cost

**More difficult:**
- No single-tenant compute isolation — workloads share underlying infrastructure with other tenants (relevant if compliance requirements change; would require migration to Workload Profiles v2 ACE with Dedicated plan)
- Replica resource spec (2 vCPU / 4 GiB) is a best-effort estimate; incorrect sizing may require a redeployment to adjust, as resource limits are set at the revision level
- At sustained high scale (hundreds of replicas running continuously for extended periods), Dedicated may become more cost-effective — this should be revisited if workload patterns shift towards steady throughput
