# Redis Hosting for the Atlas Distributed Cache — Azure Managed Redis vs. Redis on Azure Container Apps

## Status

Proposed.

## Context

The team has decided to introduce a distributed cache to close the caching gaps identified across the search process. The as-is analysis confirms that
**every cache in Atlas today lives in the memory of a single process** — there is no Redis, no distributed cache, and no shared cache store of any kind in
the codebase. That produces four concrete problems, all amplified by the Match Prediction Worker's KEDA scale-out from 0 to 50 replicas: redundant
per-replica loads of the same reference data, cold-start latency on scale-out, avoidable read pressure on SQL and Azure Table Storage, and one confirmed
correctness defect (a haplotype-frequency-set import only clears the importing process's own cache, so other replicas serve a stale active-set list for up
to `ActiveSetCacheExpiryMinutes`).

Two upstream decisions are already settled and are **not** revisited here:

1. **A distributed cache is wanted**, in an L1 (in-process) + L2 (shared) shape behind a cache-aside abstraction, with versioned keys, degrading to the
   source store when the cache is unavailable. Tracked under epic ATL-189 → ATL-190 (the `IDistributedCacheProvider` abstraction), ATL-194/ATL-195
   (Match Prediction consumers), ATL-196/ATL-197 (HLA Metadata Dictionary and MAC Dictionary consumers — the latter since descoped, see below).
2. **Redis will be Azure Managed Redis (AMR), not Azure Cache for Redis (ACR)**, because ACR is retiring: Basic/Standard/Premium retire 30 September 2028
   (instances disabled from 1 October 2028), and Enterprise/Enterprise Flash retire 31 March 2027 (disabled from 1 April 2027). Microsoft's guidance is to
   move to AMR now rather than wait for the deadline.

**Which datasets actually go into L2** is governed by the current high-level design, *Atlas — Discovery — Caching Architecture v2*, which treats caching as a
cross-cutting capability spanning Matching, Scoring, Match Prediction and Data Refresh, and deliberately narrows L2 scope rather than moving every in-memory
cache into Redis:

| Dataset | Target approach | Direction |
|---|---|---|
| **HLA Metadata Dictionary** | **L1 + L2** — the primary distributed-cache use case, with a 12-hour L2 lifetime, invalidation only after a successful Data Refresh, and cache identity qualified by nomenclature version | **Must** |
| Haplotype frequency sets (the large per-set tables) | Primarily local; reassess around Data Refresh | Should / limited |
| Imputed genotype sets | Potential L1 + L2, pending validation of benefit, key dimensions and TTL | Could |
| MAC codes | L1 / local memory only | **Not needed in L2** |

That scope is what this ADR sizes and evaluates against. Note that the "haplotype frequency sets" row refers to the *large* per-set frequency tables
(ATL-195), not to the small active-set lookup key (ATL-194) — the latter is a handful of bytes of control-plane data and is the fix for the one confirmed
correctness defect, so it remains a genuine L2 consumer regardless of that row.

This ADR decides the remaining, narrower question raised by ATL-345: **should Redis be consumed as an Azure-managed PaaS offering, or self-hosted as a
container on Azure Container Apps (ACA)?** ACA is a plausible candidate precisely because Atlas already runs there — Phase3/001 adopted ACA over AKS for
containerised workloads, and Phase3/002 placed the Match Prediction Worker on the ACA Consumption plan — so a Redis container would land on a platform the
team already owns and pays for, with no new service to learn.

### Current infrastructure, as provisioned (`terraform/core`)

These facts constrain both options and were confirmed directly against the Terraform in this repository, not assumed:

| Fact | Evidence |
|---|---|
| There is **no virtual network anywhere in Atlas** — no VNet, subnet, private endpoint, or private DNS zone | No `azurerm_virtual_network`, `azurerm_subnet`, `azurerm_private_endpoint`, or `azurerm_private_dns_*` resource exists in `terraform/core` |
| The ACA Environment is a **legacy "Consumption-only" environment on the platform-managed network** | `azurerm_container_app_environment.atlas` (`container_apps_environment.tf`) sets no `infrastructure_subnet_id` and declares no `workload_profile` block |
| That environment is protected against replacement | `lifecycle { prevent_destroy = true }` on the same resource |
| Only **one** Atlas workload runs inside that environment: the Match Prediction Worker | `modules/match_prediction/container_app.tf` is the sole `azurerm_container_app`; Donor Import, Matching Algorithm, Match Prediction (Functions), Repeat Search, Search Tracking and the Functions apps all run on the shared Elastic Premium plan (`azurerm_service_plan.atlas-elastic-plan`) |
| The Worker is sized at exactly the Consumption-only ceiling | `CONTAINER_CPU = "2.0"`, `CONTAINER_MEMORY = "4Gi"`, `CONTAINER_MAX_REPLICAS = "50"` in `dev/uat/live.tfvars` and `wmda-*.tfvars` |
| Existing data services are reached over **public endpoints** with TLS and credential-based auth | `azurerm_mssql_firewall_rule.firewall_rule_allow_azure` opens `0.0.0.0` ("Allow Azure services"); storage accounts are public with `min_tls_version = "TLS1_2"`; Service Bus is reached via namespace connection strings |
| Region is `uksouth` by default, `westeurope` for the WMDA installations | `variables.tf` (`LOCATION` default) and `wmda-live.tfvars` / `wmda-uat.tfvars` |

### Who needs to reach the cache

This matters more than any other factor, so it is stated up front. The L2 consumers do **not** all live in the same compute:

| Consumer | Ticket | v2 direction | Runs in |
|---|---|---|---|
| HLA Metadata Dictionary table repositories (9 classes) | ATL-196 | **Must** | Matching Algorithm Functions, Repeat Search Functions, Match Prediction — **Elastic Premium plan, outside the ACA environment** |
| `HaplotypeFrequencyCache` active-set lookup | ATL-194 | Must (correctness fix) | Match Prediction Worker (**inside the ACA environment**) and `Atlas.Functions` |
| Per-set haplotype frequency version markers | ATL-195 | Should / limited | Match Prediction Worker (**inside the ACA environment**) |
| Imputed / converted genotype sets (candidate, not yet ticketed) | — | Could | Match Prediction Worker and `Atlas.Functions` |
| Matching `ScoringCache` (candidate, not yet ticketed; not listed in v2) | — | Not yet scoped | Matching Algorithm Functions, Repeat Search — **Elastic Premium plan, outside the ACA environment** |
| MAC Dictionary `MacCacheService` | ATL-197 | **Not needed in L2** | (out of scope per v2) |

The critical row is the first one. **The single "Must" consumer — the HLA Metadata Dictionary, which v2 calls "the primary distributed-cache use case" — runs
entirely on the Elastic Premium plan, outside the Container Apps environment.** A cache reachable only from inside that environment cannot serve it at all.

## Investigation

### 1. Working profile

The profile below is derived from the datasets v2 actually puts in L2, rather than from a generic "cache sizing" template. Sizes marked *(estimate)* are
order-of-magnitude derivations that should be replaced with measurements once ATL-190's abstraction exists and the HMD consumer is instrumented; they are
deliberately conservative (biased high) so the SKU choice is not invalidated by being wrong in the expensive direction.

| Dataset | v2 direction | Entries | Value shape | Bytes *(estimate)* | Read pattern | Write pattern | Mutability |
|---|---|---|---|---|---|---|---|
| **HMD metadata tables (ATL-196)** | **Must** | 9 repository classes × 1 table each, per nomenclature version | Whole serialised table per key | **200–500 MB per nomenclature version** | Cold start and post-refresh only — L1 serves steady state | Written once by the first instance to build each table | **Immutable per version**; 12 h L2 TTL, invalidated only after a successful Data Refresh |
| Active HF set lookup (ATL-194) | Must (correctness fix) | 1 key — a `(RegistryCode, EthnicityCode) → HaplotypeFrequencySet` map | Small serialised collection | < 100 KB | Every Match Prediction request, behind a short L1 TTL | Invalidated on HF set import (rare) | Rare, explicit invalidation |
| HF set version markers (ATL-195) | Should / limited | 1 per HF set — tens | Integer / version string | < 10 KB total | Cheap check before trusting the local copy | Incremented on import | Rare |
| Imputed / converted genotype sets (candidate) | Could | One per `(donor, AllowedLociKey, HF set version, nomenclature version)` | Moderate object | Grows with searched-donor population — open-ended | Read once per replica per donor within a burst | Write-on-compute | Immutable per key |
| `ScoringCache` (candidate, not in v2) | Not yet scoped | Very high — one key per `(locus, donor HLA, patient HLA, nomenclature version)` | `MatchGrade` / `MatchConfidence` / `bool?` — a few bytes behind a ~60–80 byte key | Open-ended | Extremely high op rate, tiny values | Write-on-compute | Deterministic and immutable per nomenclature version |
| MAC dictionary | **Not needed in L2** | — | — | — (~567,000 codes stay local) | — | — | — |

Aggregating that into the profile that actually drives SKU selection:

| Dimension | Value | Reasoning |
|---|---|---|
| **Working set for the "Must" scope (HMD + active-set lookup)** | **~200–500 MB per live nomenclature version; allow ~1 GB to cover a refresh rollover** | HMD is the primary and first L2 consumer. Because keys are nomenclature-version-qualified and the 12 h TTL is the only thing that retires the old version, both the outgoing and incoming version can be resident simultaneously across a Data Refresh — so the sizing target is roughly two versions, not one. |
| **Working set if the "Could" candidates are added** | **Open-ended — must be capped by policy, not by SKU** | Imputed genotype sets and `ScoringCache` are unbounded key spaces. The correct control is a TTL plus an all-keys eviction policy; sizing a memoisation cache to "fit everything" is not a coherent goal. |
| **Throughput** | **Low. Cold-start-and-refresh-driven, not per-request** | HMD's L2 reads happen on cold start and after a refresh; L1 serves steady state, and ATL-196 explicitly does not touch the hot read path. The active-set lookup is one cheap read behind an L1 TTL. The one genuine throughput hazard is `ScoringCache` — called per locus per donor per search, where a per-operation network round trip would cost more than the calculation it caches. That is a design constraint on ATL-190's consumers, not a reason to buy a bigger SKU. |
| **Value size** | **Large individual values (whole serialised tables), not many small ones** | This is worth stating because it is the opposite of a typical session-cache profile: a handful of multi-megabyte values dominate, which favours memory headroom and network bandwidth over operations-per-second — i.e. the Balanced or Memory Optimized tiers, not Compute Optimized. |
| **Concurrent connections** | **Low hundreds, worst case** | 50 Match Prediction Worker replicas + the Elastic Premium fleet (`MATCHING_MAX_SCALE_OUT = "1"` today; ATL-196 assumes up to 3 Matching instances) + Functions apps. `StackExchange.Redis` multiplexes, so this is roughly 2–4 connections per process, not per request. Every candidate SKU allows 15,000. |
| **Persistence** | **Not required** | The design is explicitly cache-aside: L2 "must not become the system of record", and an L2 failure falls back to the authoritative source. Every cached value is reconstructible from SQL or Table Storage. Persistence would add cost and operational surface for no correctness benefit. |
| **Eviction** | **Required** | With version-qualified keys, entries for a superseded nomenclature version are never read again and are never explicitly deleted; they must be retired by TTL or eviction. |
| **HA / failover tolerance** | **A short failover window is acceptable in production; a cold cache is not a correctness problem, but a fallback storm is a load problem** | L2 sits behind an existing L1, and every miss falls back to the source of truth. What must be protected against is not the outage itself but the thundering herd of fallback reads hitting SQL/Table Storage when a cold L2 coincides with a 50-replica scale-out — a risk v2 names explicitly ("avoid uncontrolled fallback from many instances simultaneously overwhelming SQL or Table Storage"). |
| **Compliance posture** | **Match the existing Atlas baseline as a minimum, and prefer better** | Atlas holds no cache data more sensitive than what is already in the SQL databases and Table Storage those caches read from — but HLA typing is personal health data, so encryption in transit and no anonymous access are non-negotiable regardless of tier. |

**The two most important consequences of this profile:** the workload is a small number of large, immutable, version-keyed values read on cold start — a few
hundred MB and a low operation rate, with no throughput argument for self-hosting anywhere in it; and its single "Must" consumer sits outside the Container
Apps environment. The decision therefore turns on reachability, platform durability, and total cost of ownership rather than on performance.

### 2. Reachability — the decisive constraint

**Redis on ACA can only be reached by workloads inside the ACA environment, unless Atlas builds a VNet.**

Redis speaks a TCP protocol, not HTTP, so an ACA-hosted Redis needs TCP ingress. Microsoft's documentation is explicit on the split: *internal* TCP ingress
works without a custom VNet, but *external* TCP ingress "requires a custom VNet. If you try to create an external TCP app without a custom VNet, you receive
a `ContainerAppTcpRequiresVnet` error."

So in Atlas's environment as it stands today:

- **Internal TCP ingress** works, costs nothing extra, and is network-isolated by construction — but it is reachable *only from other container apps in the
  same environment*. That is the Match Prediction Worker and nothing else. **ATL-196 — the sole "Must" consumer, the one v2 calls the primary
  distributed-cache use case, and the ticket whose entire purpose is to stop nine repository classes re-crawling Table Storage on every cold start — runs on
  the Elastic Premium plan and could not connect.** Nor could the `ScoringCache` candidate. What remains reachable is ATL-194 and ATL-195, i.e. a
  sub-megabyte control-plane cache: real value, since ATL-194 fixes a live correctness defect, but not the architecture v2 describes.
- **External TCP ingress** would require a custom VNet on the Container Apps environment. And the network type is fixed at creation: *"After you create an
  environment with either the default Azure network or an existing virtual network, you can't change the network type."* Atlas's environment was created on
  the default platform network and carries `prevent_destroy = true`. Getting VNet-integrated ACA therefore means **recreating the Container Apps
  environment** — a disruptive change to the one piece of infrastructure the team has explicitly fenced off, and one that would also want migrating from the
  legacy Consumption-only environment type to Workload Profiles v2 (Consumption-only environments do not support UDRs, NAT Gateway egress, or environment
  private endpoints, and Microsoft now describes them as legacy).

By contrast, **AMR is reachable by every consumer without touching the ACA environment at all.** Its endpoint is a hostname on port 10000 with TLS, which
any of the Function Apps, the Functions host, and the Worker can dial. Two network postures are available:

| Posture | Works with today's Atlas infra? | Security relative to current Atlas baseline |
|---|---|---|
| AMR with `public_network_access = "Enabled"`, TLS-encrypted client protocol, Entra ID auth, access keys disabled | **Yes, immediately** | **Equivalent or better.** Atlas already reaches Azure SQL through an "Allow Azure services (0.0.0.0)" firewall rule and Storage over public endpoints; AMR adds Entra-based auth in place of connection-string secrets. Note that AMR is documented as not supporting VNet injection or IP-based firewall rules, so public access is effectively all-or-nothing — confirm the state of the portal Firewall blade at provisioning time, as the troubleshooting documentation does reference firewall rules. |
| AMR with a private endpoint and `public_network_access = "Disabled"` | **No — needs new infrastructure** | **Strictly better than anything in Atlas today**, and the target state. Requires a VNet, a private DNS zone, and regional VNet integration for the Elastic Premium apps; the Match Prediction Worker would additionally need the ACA environment recreated with VNet integration, per above. |

This is worth stating plainly because it affects an existing ticket: **ATL-214's acceptance criterion "Resource is provisioned behind a private endpoint" is
not achievable in Atlas's current infrastructure.** It presumes a VNet that does not exist, and for the Match Prediction Worker specifically it presumes an
ACA environment that would have to be rebuilt. That is a legitimate target state, but it is a separate workstream and should not gate the first cache
consumer.

### 3. Durability, HA, and operational posture

| Factor | Weight | Azure Managed Redis | Redis self-hosted on ACA |
|---|---|---|---|
| **Reachable by all six candidate consumers** | **Decisive** | Yes — TLS endpoint on port 10000, callable from Functions and Container Apps alike | **No.** Internal TCP ingress reaches only the Match Prediction Worker; anything wider needs the ACA environment recreated with a VNet |
| **Platform fit for the workload** | **High** | Purpose-built stateful service | ACA is explicitly *"designed for stateless workloads"*; it does not replicate application data between zones, and ephemeral storage "is deleted when the container or replica is shut down" |
| **HA / failover** | **High** | HA deploys ≥ 2 nodes, zone-distributed by default in AZ regions; unplanned failover is typically 10–15 s of interruption with automatic detection and traffic rerouting | **None.** A single replica is a single point of failure; a restart is a full cache flush. Running >1 replica does not give a Redis cluster — it gives N independent Redis servers behind a load balancer, which is a correctness bug, not redundancy |
| **SLA** | **High** | Covered by the Azure SLA for cache endpoint connectivity when HA is enabled | **No SLA on the Redis service.** ACA's SLA covers the platform, not the durability of the data in your container |
| **Behaviour under platform maintenance** | **High** | Maintenance creates new nodes and fails over; clients see transient faults and retry. Scheduled maintenance windows are available (preview) | Replica is restarted at times the team does not control, with total data loss each time. Every revision change also restarts it |
| **Capacity ceiling** | **Medium** | Balanced tier spans 0.5 GB to 960 GB; scaling memory size and performance tier is a supported in-place operation | **Hard-capped at 4 GiB per replica**, because apps on the Consumption plan in a Consumption-only environment "are limited to a maximum of 2 cores and 4Gi of memory" — and less than 4 GiB is actually usable after container overhead. Raising this requires the environment rebuild described above |
| **Persistence / backup** | Low *(Atlas needs neither)* | RDB (1h/6h/12h) or AOF (1s) persistence available on all tiers; import/export to Blob Storage | Would require an Azure Files (SMB) mount, since NFS mounts need a custom VNet. Running Redis AOF/RDB over an SMB share is not a configuration to rely on for durability |
| **Ops burden** | **High** — Phase3/001 weighted this heavily, for one DevOps engineer | Patching, Redis version upgrades, node replacement, and health monitoring are Microsoft's. Redis 7.4.x today, with a supported upgrade path | Atlas takes ownership of a third-party image: mirroring `redis` into the shared ACR, tracking CVEs, rebuilding and redeploying on every Redis security release, and owning `redis.conf`, `maxmemory` policy, and monitoring. This is exactly the ongoing-ownership cost Phase3/001 rejected AKS to avoid |
| **Auth model** | **High** | Microsoft Entra ID with managed identity; access-key auth is off by default in the Terraform resource (`access_keys_authentication_enabled` defaults to `false`) | Redis AUTH with a password held as a container-app secret — reintroducing exactly the secret sprawl the ATL-189 design set out to avoid |
| **Observability** | Medium | First-class Azure Monitor metrics, connection audit logs via diagnostic settings, alerting on cache metrics | Log Analytics captures container stdout; any Redis metric (hit ratio, evictions, memory) needs an exporter the team builds and runs |
| **Works with the already-chosen client** | Medium | `StackExchange.Redis` — the client ATL-190 has already selected — connects to AMR's clustered-by-default topology with no special configuration | Also works, and single-node is simpler for the client. Genuine, if minor, point in ACA's favour |
| **Scale-out burst behaviour (0→50 replicas)** | **High** | 50 replicas × ~2–4 multiplexed connections is a rounding error against B0/B1's 15,000-connection limit; the shared cache is what *fixes* the burst problem, since the first replica to compute a value serves all the others | The cache is on the same Consumption compute that is contending during the burst, and the burst is precisely when the shared cache matters most |
| **Cost** | **High** | See below — **cheaper**, which inverts the usual self-hosting intuition | See below |
| **Long-term viability** | Medium | AMR is the Azure-first-party successor product; ACR's retirement dates are the reason it was chosen | Redis OSS licence changes are what pushed Azure to Redis Enterprise in the first place; self-hosting means owning that question |

**Where the ACA option genuinely wins:** it needs no new Azure resource and no cost sign-off; internal ingress is network-isolated with no VNet or private
endpoint work; a single non-clustered Redis is marginally simpler for a client library; and the team already knows the platform. These are real, and they
are why the option deserved evaluation rather than dismissal. They are not enough to overcome "cannot be reached by the one 'Must' consumer, is capped at
4 GiB against a 200–500 MB-per-version dataset that needs room for two versions, has no HA, no SLA, and — as shown next — costs more."

### 4. Cost estimate

Rates below were retrieved from the **Azure Retail Prices API on 4 September 2026** for `armRegionName = uksouth`, pay-as-you-go, USD. They exclude any
enterprise agreement discount. Monthly figures use 730 hours.

**Azure Managed Redis (`Balanced` tier — memory sizes 0.5 / 1 / 3 / 6 / 12 GB):**

| SKU | Memory | Usable (~80%) | $/hour | $/month | $/month, HA disabled (~50%) |
|---|---|---|---|---|---|
| `Balanced_B0` | 0.5 GB | ~0.4 GB | 0.018 | **13.14** | ~6.57 |
| `Balanced_B1` | 1 GB | ~0.8 GB | 0.037 | **27.01** | ~13.50 |
| `Balanced_B3` | 3 GB | ~2.4 GB | 0.075 | **54.75** | ~27.38 |
| `Balanced_B5` | 6 GB | ~4.8 GB | 0.179 | **130.67** | ~65.34 |
| `Balanced_B10` | 12 GB | ~9.6 GB | 0.362 | **264.26** | ~132.13 |
| `MemoryOptimized_M10` | 12 GB | ~9.6 GB | 0.248 | **181.04** | ~90.52 |

AMR reserves roughly 20% of memory for system operations, hence the usable column. The HA-disabled column applies Microsoft's documented "disabling high
availability … halves the cost" guidance to the API rate; the retail API exposes a single meter per SKU, so **confirm the non-HA rate on the first invoice**
rather than treating that column as quoted. Reservations (1- and 3-year) are also available and would reduce steady-state cost further once usage is proven;
they are deliberately not assumed here.

**Redis self-hosted on ACA (Consumption plan, uksouth):** billed at $0.000034 per vCPU-second active, $0.000004 per vCPU-second idle, and $0.000004 per
GiB-second. A cache must be running whenever anything might read it, so `min_replicas` cannot be 0 — scale-to-zero, the Consumption plan's main economic
advantage, is unavailable to this workload by definition. ACA's idle rate requires a replica to use less than 0.01 vCPU and receive less than 1,000 bytes per
second, so a Redis actually serving traffic is billed at the active rate during working hours.

| Replica size | Always idle (floor) | 12h active / 12h idle (realistic) | Always active (ceiling) |
|---|---|---|---|
| 1 vCPU / 2 GiB | 31.53 | **70.96** | 110.38 |
| 2 vCPU / 4 GiB (Consumption-only max) | 63.07 | **141.91** | 220.75 |

The Consumption free grant (180,000 vCPU-seconds and 360,000 GiB-seconds per subscription per month) does not materially offset this: Phase3/002 already
records that the free tier "is exhausted within minutes at hundreds of replicas" by the Match Prediction Worker sharing the same subscription. Azure Files
storage for any persistence attempt, and the engineering time to build and maintain an image pipeline, are additional and are not costed here.

**Comparison at the sizing this workload actually calls for:**

| Option | Monthly (uksouth, PAYG, USD) | What you get |
|---|---|---|
| **AMR `Balanced_B3`, HA enabled — recommended for production** | **~55** | 3 GB (~2.4 GB usable), comfortable room for two nomenclature versions of HMD data, ≥2 nodes zone-distributed, SLA-backed, patched, Entra auth, 15,000 connections, **reachable by every consumer** |
| AMR `Balanced_B1`, HA enabled | ~27 | 1 GB (~0.8 GB usable) — sufficient only while the cache holds control-plane data (ATL-194/195); too tight once HMD lands |
| AMR `Balanced_B1`, HA disabled (dev/uat) | ~14 | 1 GB, no SLA, no HA — appropriate for non-production only |
| AMR `Balanced_B0`, HA disabled | ~7 | 0.5 GB — only viable for testing the wiring, not for holding HMD tables |
| Redis on ACA, 1 vCPU / 2 GiB | ~71 (31–110) | ~1.5 GB usable, single point of failure, no SLA, flushed on every restart, **reachable only from inside the ACA environment** |
| Redis on ACA, 2 vCPU / 4 GiB | ~142 (63–221) | Same, at the platform ceiling — and still short of two-version HMD headroom |

**Like for like, the self-hosted option costs more and delivers less.** The recommended production SKU (`Balanced_B3`, HA, ~$55/month) is *cheaper* than a
2 vCPU / 4 GiB ACA container (~$142/month realistic) that would hold less usable data, have no redundancy, and be unreachable by the primary consumer. Even
the ACA floor case (1 vCPU / 2 GiB, billed entirely at idle rates, ~$32/month) buys less capacity than `Balanced_B1` at half the resilience.

Adding a private endpoint in Phase 2 adds a small per-endpoint hourly charge plus per-GB data processing to the AMR figures (order of $10/month at list
rates — confirm via the pricing calculator, as those meters are not exposed in the region query used above); that does not change the ranking.

### 5. Implementation readiness

The Terraform work is smaller than ATL-214 assumes, and its open spike can be closed now. The pinned provider — `hashicorp/azurerm` `= 4.74.0`, per
`terraform/core/versions.tf` — already ships **`azurerm_managed_redis`**, which supersedes the deprecated `azurerm_redis_enterprise_cluster` /
`azurerm_redis_enterprise_database` pair and takes AMR's SKU names directly (`Balanced_B0` … `Balanced_B1000`, `MemoryOptimized_*`, `ComputeOptimized_*`,
`FlashOptimized_*`). No provider upgrade is needed. Relevant arguments, with the defaults that matter:

- `sku_name` — e.g. `"Balanced_B1"`.
- `high_availability_enabled` — defaults to `true`; **changing it forces a new instance**, so choose per environment at creation rather than planning to
  toggle it later.
- `public_network_access` — `"Enabled"` / `"Disabled"`, defaults to `"Enabled"`.
- `default_database.access_keys_authentication_enabled` — defaults to `false`, i.e. **Entra-only by default**, which is what ATL-214 wants.
- `default_database.client_protocol` — defaults to `"Encrypted"`.
- `default_database.clustering_policy` — defaults to `"OSSCluster"`, which is Microsoft's recommendation and needs no special `StackExchange.Redis` config.
- `default_database.eviction_policy` — defaults to `"VolatileLRU"`, which evicts only keys carrying a TTL. Set this explicitly; see the decision below.
- `default_database.persistence_*` — omit; the cache-aside design does not need persistence.
- `azurerm_managed_redis_access_policy_assignment` — grants a managed identity access, which is how the Worker and Function Apps should authenticate.

## Decision

**Adopt Azure Managed Redis (PaaS). Reject self-hosting Redis on Azure Container Apps.**

The deciding factor is not cost or convenience but **reach**: a Redis on ACA in Atlas's current environment can serve only the Match Prediction Worker,
which leaves ATL-196 — the single "Must" consumer, the one the v2 architecture calls the primary distributed-cache use case, and the ticket whose entire
justification is eliminating redundant Table Storage crawls across the Elastic Premium fleet — unable to use it. Extending its reach means recreating a
`prevent_destroy` Container Apps environment onto a VNet. That Atlas would additionally be self-hosting a stateful service on a platform Microsoft documents
as designed for stateless workloads, with no HA, no SLA, a 4 GiB ceiling against a dataset that wants room for two 200–500 MB nomenclature versions, a data
flush on every restart, a new image-patching obligation for a single DevOps engineer, and a password secret instead of managed identity — while costing more
than the recommended PaaS SKU — makes the conclusion unambiguous.

### Provisioning

1. **Resource:** `azurerm_managed_redis` in `terraform/core`, on the pinned `azurerm 4.74.0` provider. No provider upgrade required.
2. **SKU — `Balanced`, not `MemoryOptimized` or `ComputeOptimized`.** The profile is a small number of large values read at low frequency: there is no
   throughput case for Compute Optimized, and Memory Optimized starts at 12 GB (~$181/month HA), far more memory than needed. `Balanced` starts at 0.5 GB
   and covers the whole range Atlas will plausibly want.
   - **Production (live, wmda-live):** `Balanced_B3` with `high_availability_enabled = true`. ~$55/month. 3 GB (~2.4 GB usable after AMR's ~20% system
     reservation) gives comfortable headroom for two nomenclature versions of HMD data coexisting across a Data Refresh rollover, on the 200–500 MB
     per-version estimate. Its 15,000-connection limit is far above the low hundreds this fleet will open.
   - **Non-production (dev, uat):** `Balanced_B1` with `high_availability_enabled = false`. ~$14/month — non-HA is appropriate here given those instances
     carry no SLA and may lose data during maintenance, and dev/uat need not hold a full production-scale HMD dataset.
   - **If ATL-194 ships before ATL-196**, `Balanced_B1` with HA (~$27/month) is sufficient for production in the interim, since the cache then holds only
     sub-megabyte control-plane data. Scale up to B3 before ATL-196 goes live.
   - **Revisit the size on measured `Used Memory`** once HMD data is actually resident; the 200–500 MB figure is an estimate, and memory size and
     performance tier are both in-place scale operations. Note that `high_availability_enabled` is *not* an in-place change — it forces recreation — so
     the HA choice must be right per environment at creation time.
3. **Authentication:** Microsoft Entra ID via managed identity. Leave `access_keys_authentication_enabled` at its `false` default and grant access with
   `azurerm_managed_redis_access_policy_assignment`. No connection-string secret in `local.settings.template.json` or the container app's `secret` blocks.
4. **Transport:** leave `client_protocol` at `"Encrypted"`.
5. **Clustering:** leave `clustering_policy` at `"OSSCluster"`. `StackExchange.Redis` needs no special configuration for it.
6. **Persistence:** none. Every cached value is reconstructible from SQL or Table Storage, and the design already requires graceful degradation to the
   source. Persistence would add cost and operational surface for no correctness gain.
7. **Eviction policy:** set `eviction_policy = "AllKeysLRU"` explicitly rather than accepting the `VolatileLRU` default. `VolatileLRU` only ever evicts keys
   that carry a TTL. v2's design does give HMD entries a 12-hour L2 lifetime, so in the intended steady state most keys would be evictable under either
   policy — but the default makes correct behaviour under memory pressure *contingent on every writer remembering to set a TTL*, and a full cache holding
   any TTL-less entries returns write errors instead of shedding cold data. `AllKeysLRU` costs nothing, removes that contingency, and is the right policy
   for a store that is by design a pure cache with an authoritative source behind it. Set it deliberately; don't inherit it.

### Network posture — phased, deliberately

8. **Phase 1 (now):** `public_network_access = "Enabled"`, with TLS and Entra-only authentication. This is not a compromise on the existing Atlas security
   baseline — Azure SQL is already reached through an `0.0.0.0` "Allow Azure services" firewall rule and Storage over public endpoints with shared keys —
   and it is an improvement on it, since the cache uses managed identity rather than a secret. It unblocks ATL-190 and ATL-194 (which fixes a live
   correctness defect) without waiting on a networking programme.
9. **Phase 2 (separate workstream):** private endpoint with `public_network_access = "Disabled"`. This requires a VNet, a private DNS zone, and regional
   VNet integration for the Elastic Premium apps; the Match Prediction Worker additionally requires the ACA environment to be recreated with VNet
   integration, since network type is immutable after creation. **ATL-214's private-endpoint acceptance criterion should be moved out of that ticket into
   this workstream**, rather than blocking the cache foundation on infrastructure Atlas has never had. That workstream is worth doing on its own merits —
   it would also close the `0.0.0.0` SQL firewall rule — but it is a networking decision, not a caching one.

### Consumer-side constraints this decision imposes

10. **Keep large HMD values out of the hot read path.** ATL-196 is explicit that L2 is consulted on cold start and miss only, with L1 serving steady-state
    hot reads. Multi-megabyte serialised tables must not be fetched per lookup — that is the difference between this decision paying for itself and it
    making things slower.
11. **Do not put a per-operation Redis round trip on the `ScoringCache` hot path**, if that candidate is ever scoped. Its values are a few bytes behind a
    ~70-byte key and it is called per locus per donor per search; a network hop per lookup would cost more than the calculation it caches. If it is
    distributed at all, reads must be batched (pipelined or `MGET`) at a natural boundary, with L1 serving steady state.
12. **Protect the sources of truth against a fallback storm.** A cold or unavailable L2 coinciding with a 0→50-replica burst turns into 50 replicas
    simultaneously re-crawling Table Storage and SQL. Bounded concurrency or a single-flight guard on the fallback path is required — v2 names this
    explicitly, and it becomes load-bearing the moment L2 exists.
13. **ATL-305 (bounding `HaplotypeFrequencyCache`) is not superseded by any of this.** L2 solves staleness and redundant loads; it does not bound a
    replica's own memory. That ticket should still land before the ATL-233 precompute consumer reaches production.
14. **ATL-197 (MAC Dictionary L2) is out of scope per v2** ("not needed in L2") and should be closed or parked rather than built against this cache. Nothing
    in this hosting decision depends on it, and its ~567,000 entries are the largest single dataset that will *not* be sized for.

### Revisit if

- Measured `Used Memory` exceeds a `Balanced_B10`/`MemoryOptimized_M10` (12 GB) and cost becomes material — at which point Flash Optimized tiers, or
  reservations, are the next levers, not self-hosting.
- A compliance requirement mandates single-tenant compute or private-only networking on a timeline the Phase 2 workstream cannot meet.
- Atlas acquires a VNet-integrated Workload Profiles v2 Container Apps environment for other reasons, which would remove the reachability objection to
  self-hosting — though every other objection (HA, SLA, statefulness, patching, cost) would still stand.

## Consequences

**Easier:**

- Every L2 consumer, on either compute platform, can share one cache — so ATL-196, the "Must" consumer that a Redis inside the Container Apps environment
  could not have served at all, becomes deliverable rather than blocked on compute topology.
- ATL-194's live correctness defect — replicas serving a stale active-set list after an import — is fixable now, without waiting on any networking work.
- ATL-214's open spike is closed: the resource is `azurerm_managed_redis`, available on the currently pinned provider, and its defaults already satisfy the
  ticket's Entra-ID/no-access-keys requirement.
- No new Redis operational surface: patching, version upgrades, node replacement, failover and zone distribution are Microsoft's responsibility — consistent
  with the reasoning that chose ACA over AKS in Phase3/001.
- Cost is low, predictable, and scales with a supported in-place operation rather than a re-provisioning: ~$14/month per non-production environment and
  ~$55/month for production, with memory size adjustable on measured usage.
- The Container Apps environment is untouched — no change to a `prevent_destroy` resource, and no migration off the Consumption-only environment type as a
  prerequisite.

**More difficult / risks:**

| Risk | Severity | Mitigation |
|---|---|---|
| The cache is publicly addressable in Phase 1, protected by TLS and Entra ID but with no IP-level restriction (AMR does not support VNet injection or IP firewall rules) | **Medium** | Entra-only auth with access keys disabled; enable connection audit logs via diagnostic settings; Phase 2 private endpoint. Confirm at provisioning whether the portal Firewall blade offers usable IP rules, as documentation is inconsistent on this point |
| A new paid Azure resource requires cost sign-off before provisioning | **Medium** | Figures above are from the Azure Retail Prices API for uksouth; the production line item is ~$55/month (`Balanced_B3`, HA) and ~$14/month per non-production environment (`Balanced_B1`, non-HA). ATL-214 already carries the sign-off acceptance criterion |
| The 200–500 MB HMD-per-version estimate is unmeasured, so `Balanced_B3` could prove under- or over-sized | **Medium** | Memory size is an in-place scale operation in both directions (with documented restrictions on scaling down), so this is recoverable. Instrument `Used Memory` from the first ATL-196 deployment and right-size before HMD goes live in production |
| `eviction_policy` left at the `VolatileLRU` default causes write failures on a full cache under versioned keys | **Medium** | Set `AllKeysLRU` explicitly in Terraform and assert it in the review of ATL-214's PR |
| A cold or unavailable L2 during a 50-replica burst produces a fallback storm against SQL/Table Storage | **Medium** | Bounded-concurrency or single-flight guard on the fallback path (design decision D3), specified as part of ATL-190's abstraction rather than left to each consumer |
| Failover or maintenance produces a 10–15 s window of transient faults | **Low** | Cache-aside with retry and circuit-breaker on the client; a miss is a slow read, not a failed search. Scheduled maintenance windows (preview) can further constrain when this happens |
| Non-HA dev/uat instances can lose data during maintenance and carry no SLA | **Low** | Accepted deliberately for non-production; production runs HA. Note that `high_availability_enabled` cannot be toggled in place — it forces recreation — so the per-environment choice must be made at creation |
| Integration tests need a Redis, and the repo has no local Redis provision | **Low** | Already flagged on ATL-194. A container-based Redis for local/CI integration testing is a legitimate and unrelated use of a Redis container — this ADR rejects self-hosting for the *production cache tier*, not for test fixtures |
| Regional availability must be confirmed for both `uksouth` and `westeurope` (WMDA installations) before provisioning | **Low** | Check AMR availability and the specific SKU's status per region; note that Balanced sizes above 350 GB are in preview, which is far above anything proposed here |
