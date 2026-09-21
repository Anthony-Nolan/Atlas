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

This ADR decides the remaining, narrower question raised by ATL-345: should Redis be consumed as an Azure-managed PaaS offering, or self-hosted as a
container on Azure Container Apps (ACA)? ACA is a plausible candidate precisely because Atlas already runs there. Phase3/001 adopted ACA over AKS for
containerised workloads, and Phase3/002 placed the Match Prediction Worker on the ACA Consumption plan, so a Redis container would land on a platform the
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

None of the above is treated as a hard blocker in this ADR. The team has confirmed that the `prevent_destroy` lock can be removed, that a dedicated
Container Apps environment with workload profiles can be created if a Redis container needs one, and that introducing a VNet is acceptable. The team has
also noted that Atlas infrastructure remains publicly reachable at this phase of the project, so private-only networking is not itself a requirement. These
facts are therefore recorded as work items and running costs attached to each option, not as reasons an option is impossible. The comparison below is made
on that basis: both options are assumed buildable, and the question is which is better.

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

The row that shapes the comparison is the first one. The single "Must" consumer, the HLA Metadata Dictionary, which v2 calls "the primary
distributed-cache use case," runs entirely on the Elastic Premium plan, outside the Container Apps environment. A cache reachable only from inside that
environment cannot serve it, which is what makes the networking prerequisites in §2 a real cost of the self-hosted option rather than a detail.

## Investigation

### 1. Working profile

The profile below is derived from the datasets v2 actually puts in L2, rather than from a generic "cache sizing" template. Sizes marked *(estimate)* are
order-of-magnitude derivations that should be replaced with measurements once ATL-190's abstraction exists and the HMD consumer is instrumented. They are
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

Two consequences of this profile matter most. The workload is a small number of large, immutable, version-keyed values read on cold start, a few hundred MB
and a low operation rate, with no throughput argument for self-hosting anywhere in it. And its single "Must" consumer sits outside the Container Apps
environment. The decision therefore turns on reachability, platform durability, and total cost of ownership rather than on performance.

### 2. Reachability — a solvable cost, not a blocker

Reachability looks decisive at first glance, but it isn't: the constraint is real and documented, but it is removable, and the team has confirmed the
removal is acceptable. This section sets out what the documentation actually says, what configuration works, and what that configuration costs. The
conclusion is that reachability is a work item priced into the comparison, not a veto.

Redis speaks a TCP protocol, not HTTP, so an ACA-hosted Redis needs TCP ingress, and TCP ingress has narrower rules than HTTP ingress. Here is what the
documentation says, verbatim:

1. *"External TCP ingress is only supported for Container Apps environments that use a virtual network."* — and the CLI reference adds: *"External TCP
   ingress requires a custom VNet. If you try to create an external TCP app without a custom VNet, you receive a `ContainerAppTcpRequiresVnet` error.
   Internal TCP ingress works without a custom VNet."*
2. With TCP ingress enabled, a container app *"is accessible to other container apps in the same environment via its name … and exposed port number"* and
   *"is accessible externally via its fully qualified domain name (FQDN) and exposed port number **if the ingress is set to `external`**."*
3. Environment network type is immutable: *"After you create an environment with either the default Azure network or an existing virtual network, you can't
   change the network type."*

Redis on ACA can only be reached by workloads inside the ACA environment unless Atlas builds a VNet. That claim is accurate as written, and the "unless"
clause is the operative half. Its corollary is equally true and is the more useful framing: with a VNet, an ACA-hosted Redis is reachable by every Atlas
consumer.

The configuration that works is spelled out below, because one part of it is counter-intuitive:

- Create the environment as **internal** (`--internal-only true`) on a custom VNet subnet (`/27` or larger for a workload profiles environment).
- Give the Redis app **`--type external`** ingress with `transport: tcp`. This is the counter-intuitive part: on an internal environment, `external` does
  **not** publish to the internet — *"On an internal environment, **Accepting traffic from anywhere** doesn't publish your app to the internet, because the
  environment has no public endpoint. Instead, it publishes the app at the environment's internal load balancer so that clients in your virtual network can
  reach it."* Conversely, `--type internal` would be **too** restrictive: *"Reachable only from other apps in the same environment. Clients elsewhere in the
  virtual network receive an HTTP 404 response."*
- Give the Elastic Premium apps **regional VNet integration**, which is supported on Elastic Premium and grants access to *"resources in the same virtual
  network as your app"*. Requires a `/28` or larger subnet, same region as the apps; Microsoft recommends `/24` for Windows or `/26` for Linux Elastic
  Premium plans to avoid scale-related address exhaustion. Atlas has a single shared plan, so one integration subnet suffices (a plan supports up to two,
  and apps from different plans can't share one).

That is a working design. It is also net-new infrastructure: a VNet, at least two subnets, an ACA environment rebuilt or replaced, and VNet integration
added to every Function App.

There is an asymmetry worth pointing out here. The team's position is that Atlas remains publicly reachable at this phase, which removes private networking
as a requirement. But the VNet is not a privacy choice, it is a hard platform prerequisite for external TCP ingress. So the option that looks like "reuse
infrastructure we already own" is the one that needs new networking, while the option that looks like "add a new Azure service" needs none:

| | Redis on ACA | Azure Managed Redis |
|---|---|---|
| VNet required to be reachable by all consumers | **Yes** — platform requirement for external TCP ingress | **No** at this phase; a public endpoint with TLS and Entra ID works today |
| ACA environment change | **Yes** — recreate the existing one, or stand up a dedicated workload profiles environment | **None** |
| VNet integration on the Elastic Premium apps | **Yes** | **None** at this phase; needed only for the optional private-endpoint target state |
| Reachable by the "Must" consumer (ATL-196, on Elastic Premium) | Only once the above is built | Immediately |

One caution on the "dedicated ACE" variant: standing up a *separate* environment for Redis, leaving the Match Prediction Worker in the existing
Consumption-only environment, does not work privately. The existing environment has no VNet, so it cannot reach an internal load balancer in the new one.
Making Redis reachable from it would require the Redis environment to be external with external TCP ingress, i.e. Redis published on a public IP and TCP
port, protected only by Redis `AUTH` with a password held as a container-app secret. That is a weaker posture than AMR's public endpoint, which is a managed
service with Entra ID authentication, TLS by default, and connection audit logs. If the dedicated-environment route is taken, the Worker should move into
the new VNet-integrated environment too, rather than reaching Redis over the internet.

**AMR's network postures**, for completeness:

| Posture | Works with today's Atlas infra? | Security relative to current Atlas baseline |
|---|---|---|
| `public_network_access = "Enabled"`, TLS client protocol, Entra ID auth, access keys disabled | **Yes, immediately** | **Equivalent or better.** Atlas already reaches Azure SQL through an "Allow Azure services (0.0.0.0)" firewall rule and Storage over public endpoints with shared keys; AMR adds Entra-based auth in place of connection-string secrets. Note AMR supports neither VNet injection nor IP-based firewall rules, so public access is effectively all-or-nothing — confirm the state of the portal Firewall blade at provisioning, since the troubleshooting documentation does reference firewall rules. |
| Private endpoint with `public_network_access = "Disabled"` | Needs the same VNet work the ACA option needs anyway | **Strictly better than anything in Atlas today.** The sensible target state once a VNet exists for other reasons |

This still affects an existing ticket: ATL-214's acceptance criterion "Resource is provisioned behind a private endpoint" cannot be met without net-new
networking, and the team has confirmed private networking isn't required at this phase. It should move to a
separate networking workstream rather than gate the first cache consumer. If Atlas builds that VNet anyway, AMR should adopt the private endpoint at that
point.

### 3. Durability, HA, and operational posture

| Factor | Weight | Azure Managed Redis | Redis self-hosted on ACA |
|---|---|---|---|
| **Reachable by every consumer** | **High** — a cost, not a veto | Yes, with no new networking — TLS endpoint on port 10000, callable from Functions and Container Apps alike | Yes, **once a VNet, a rebuilt or new workload profiles environment, and VNet integration for the Function Apps are in place**. Internal TCP ingress alone reaches only the Match Prediction Worker |
| **Platform fit for the workload** | **High** | Purpose-built stateful service | ACA is explicitly *"designed for stateless workloads"*; it does not replicate application data between zones, and ephemeral storage "is deleted when the container or replica is shut down" |
| **HA / failover** | **High** | HA deploys ≥ 2 nodes, zone-distributed by default in AZ regions; unplanned failover is typically 10–15 s of interruption with automatic detection and traffic rerouting | **Not available out of the box; achievable only by hand-building a Redis topology.** See the analysis immediately below this table |
| **SLA** | **High** | Covered by the Azure SLA for cache endpoint connectivity when HA is enabled | **No SLA on the Redis service.** ACA's SLA covers the platform, not the durability of the data in your container |
| **Behaviour under platform maintenance** | **High** | Maintenance creates new nodes and fails over; clients see transient faults and retry. Scheduled maintenance windows are available (preview) | Replica is restarted at times the team does not control, with total data loss each time. Every revision change also restarts it |
| **Capacity ceiling** | **Medium** | Balanced tier spans 0.5 GB to 960 GB; scaling memory size and performance tier is a supported in-place operation | **4 GiB per replica today**, because apps on the Consumption plan in a *Consumption-only* environment "are limited to a maximum of 2 cores and 4Gi of memory". A workload profiles environment raises this to **8 GiB** on the Consumption profile (0.25–4 vCPU / 0.5–8 GiB), or up to 256 GiB on a Dedicated E-series profile — but ACA fixes memory at a 1:2 ratio to vCPU, so more memory is only purchasable by also buying vCPU the workload does not need. See the cost section |
| **Persistence / backup** | Low *(Atlas needs neither)* | RDB (1h/6h/12h) or AOF (1s) persistence available on all tiers; import/export to Blob Storage | Would require an Azure Files (SMB) mount, since NFS mounts need a custom VNet. Running Redis AOF/RDB over an SMB share is not a configuration to rely on for durability |
| **Ops burden** | **High** — Phase3/001 weighted this heavily, for one DevOps engineer | Patching, Redis version upgrades, node replacement, and health monitoring are Microsoft's. Redis 7.4.x today, with a supported upgrade path | Atlas takes ownership of a third-party image: mirroring `redis` into the shared ACR, tracking CVEs, rebuilding and redeploying on every Redis security release, and owning `redis.conf`, `maxmemory` policy, and monitoring. This is exactly the ongoing-ownership cost Phase3/001 rejected AKS to avoid |
| **Auth model** | **High** | Microsoft Entra ID with managed identity; access-key auth is off by default in the Terraform resource (`access_keys_authentication_enabled` defaults to `false`) | Redis AUTH with a password held as a container-app secret — reintroducing exactly the secret sprawl the ATL-189 design set out to avoid |
| **Observability** | Medium | First-class Azure Monitor metrics, connection audit logs via diagnostic settings, alerting on cache metrics | Log Analytics captures container stdout; any Redis metric (hit ratio, evictions, memory) needs an exporter the team builds and runs |
| **Works with the already-chosen client** | Medium | `StackExchange.Redis` — the client ATL-190 has already selected — connects to AMR's clustered-by-default topology with no special configuration | Also works, and single-node is simpler for the client. Genuine, if minor, point in ACA's favour |
| **Scale-out burst behaviour (0→50 replicas)** | **High** | 50 replicas × ~2–4 multiplexed connections is a rounding error against B0/B1's 15,000-connection limit; the shared cache is what *fixes* the burst problem, since the first replica to compute a value serves all the others | The cache is on the same Consumption compute that is contending during the burst, and the burst is precisely when the shared cache matters most |
| **Cost** | **High** | See below — **cheaper**, which inverts the usual self-hosting intuition | See below |
| **Long-term viability** | Medium | AMR is the Azure-first-party successor product; ACR's retirement dates are the reason it was chosen | Redis OSS licence changes are what pushed Azure to Redis Enterprise in the first place; self-hosting means owning that question |

#### 3a. Can an ACA-hosted Redis be made highly available?

Running more than one replica does give N independent Redis servers behind a load balancer, but calling that a blanket correctness bug overstates it. It's
worth separating what is documented from what follows from it.

Documented facts:

1. **ACA load-balances traffic across the replicas of an app.** Session affinity exists to prevent this and, with it disabled, *"ingress distributes
   requests more evenly across replicas."* The platform also exposes a `tcpConnectionPool.maxConnections` setting and circuit-breaker policies that
   temporarily remove a replica *"from the load balancing pool"* — both confirming that TCP traffic to an app is distributed across its replicas.
2. **Session affinity is unavailable for TCP.** It *"is available in single revision mode when HTTP ingress is enabled"*, and *"only supported when … the
   ingress type is HTTP."* Sticky routing is enforced with HTTP cookies. So there is no supported way to pin a client to one replica of a TCP app.
3. **ACA replicates no application state between replicas.** *"Container Apps doesn't replicate application data between zones because it's designed for
   stateless workloads. Any data that your app stores in ephemeral storage … is deleted when the container or replica is shut down."* Microsoft's own
   guidance for stateful data is to *"mount an Azure Files file share … or use other Azure services like Azure Cosmos DB or Azure SQL Database that provide
   their own cross-zone replication capabilities."*
4. **Ingress addresses the app, not the replica.** TCP ingress exposes the app by *name and exposed port* (or FQDN when external); there is no per-replica
   DNS name or stable per-replica address.

Here is what follows from that. Scaling one Redis container app to N replicas gives N independent `redis-server` processes, each with its own dataset,
sharing one load-balanced endpoint, with no way to pin a connection to one of them. The consequence for a cache is not data corruption, it is a roughly 1/N
hit rate, and reads that non-deterministically see or miss another replica's writes. For most of Atlas's L2 datasets that is a performance regression
rather than a correctness fault, since values are immutable per version and a miss simply falls through to the source. ATL-194 is the exception, since its
whole purpose is cross-instance invalidation: an invalidation applied to whichever replica the connection happened to land on, with the others still serving
the old value, is precisely the defect that ticket exists to fix. So "correctness bug" is the right label for that one use case, not for the dataset as a
whole.

Genuine HA on ACA is constructible, but only if Atlas builds and operates it. Because internal TCP ingress makes each *app* individually addressable by
name, a real topology is possible: deploy each Redis node as its own container app (`redis-0`, `redis-1`, …), each pinned to a single replica, and configure
Redis replication with Sentinel, or Redis Cluster with the cluster bus on an additional TCP port (ACA allows up to five extra ports per app, internal ones
may reuse port numbers across apps, and `36985` is reserved). Nothing in the documentation forbids this.

What it means in practice is that Atlas would be designing, deploying, and operating a self-managed Redis cluster: quorum and failover configuration,
Sentinel monitoring, node replacement, split-brain handling, on a platform that restarts replicas on its own schedule, offers no stable per-replica storage,
and whose health probes have no understanding of Redis replication state. That is a substantially larger undertaking than "run a Redis container," and it
lands on the single DevOps engineer whose limited capacity was the highest-weighted factor in Phase3/001's decision to choose ACA over AKS in the first
place. The honest framing is therefore not "impossible" but "available only by taking on exactly the class of operational ownership this team has previously
and deliberately declined."

Where the ACA option genuinely wins: it uses a platform the team already runs, with an existing deployment pipeline (`AzureContainerApps@1`), ACR, and
managed identity wiring; a single non-clustered Redis is marginally simpler for a client library; and, for the Match Prediction Worker specifically,
internal ingress inside the current environment needs no VNet at all. These are real, and they are why the option deserved evaluation rather than dismissal.
What they do not overcome is the combination that remains once reachability is bought and paid for: no managed HA, no SLA on the data, a flush on every
restart and platform maintenance event, a Redis image and configuration to patch and monitor, and, as shown next, a materially higher bill.

### 4. Cost estimate

Rates below were retrieved from the **Azure Retail Prices API on 4 September 2026** for `armRegionName = uksouth`, pay-as-you-go, GBP (queried directly in
GBP rather than converted from USD, so these are the actual Azure list prices for the region, ex VAT). They exclude any enterprise agreement discount.
Monthly figures use 730 hours.

**Azure Managed Redis (`Balanced` tier — memory sizes 0.5 / 1 / 3 / 6 / 12 GB):**

| SKU | Memory | Usable (~80%) | £/hour | £/month | £/month, HA disabled (~50%) |
|---|---|---|---|---|---|
| `Balanced_B0` | 0.5 GB | ~0.4 GB | 0.013 | **9.67** | ~4.84 |
| `Balanced_B1` | 1 GB | ~0.8 GB | 0.027 | **19.89** | ~9.94 |
| `Balanced_B3` | 3 GB | ~2.4 GB | 0.055 | **40.31** | ~20.15 |
| `Balanced_B5` | 6 GB | ~4.8 GB | 0.132 | **96.20** | ~48.10 |
| `Balanced_B10` | 12 GB | ~9.6 GB | 0.267 | **194.56** | ~97.28 |
| `MemoryOptimized_M10` | 12 GB | ~9.6 GB | 0.183 | **133.29** | ~66.64 |

AMR reserves roughly 20% of memory for system operations, hence the usable column. The HA-disabled column applies Microsoft's documented "disabling high
availability … halves the cost" guidance to the API rate; the retail API exposes a single meter per SKU, so **confirm the non-HA rate on the first invoice**
rather than treating that column as quoted. Reservations (1- and 3-year) are also available and would reduce steady-state cost further once usage is proven;
they are deliberately not assumed here.

**Redis self-hosted on ACA (Consumption plan, uksouth):** billed at £0.000025 per vCPU-second active, £0.000003 per vCPU-second idle, and £0.000003 per
GiB-second. A cache must be running whenever anything might read it, so `min_replicas` cannot be 0 — **scale-to-zero, the Consumption plan's entire economic
advantage and the reason Phase3/002 chose it for the Worker, is unavailable to this workload by definition.** ACA's idle rate additionally requires a replica
to use less than 0.01 vCPU and receive less than 1,000 bytes per second, so a Redis actually serving traffic is billed at the active rate during working
hours. The three columns below bracket that: an always-idle floor, a 12h-active/12h-idle middle case, and an always-active ceiling.

| Replica size | Environment needed | Always idle (floor) | 12h/12h (realistic) | Always active (ceiling) |
|---|---|---|---|---|
| 1 vCPU / 2 GiB | Either | 23.65 | **52.56** | 81.47 |
| 2 vCPU / 4 GiB | Either (Consumption-only max) | 47.30 | **105.12** | 162.94 |
| 4 vCPU / 8 GiB | **Workload profiles only** | 94.61 | **210.24** | 325.87 |

If the Consumption profile's 8 GiB ceiling is still not enough, the next step is a Dedicated workload profile, which is billed per node at £0.059532 per
vCPU-hour plus £0.004887 per GiB-hour, with no scale-to-zero, plus a £0.073624/hour Dedicated Plan Management charge once any Dedicated profile exists in
the environment:

| Dedicated profile | Node spec | Node cost | + plan management | **Monthly total, one node** |
|---|---|---|---|---|
| D4 (general purpose) | 4 vCPU / 16 GiB | 230.91 | 53.75 | **284.66** |
| E4 (memory optimized) | 4 vCPU / 32 GiB | 287.99 | 53.75 | **341.74** |

A workload profiles environment that uses *only* the Consumption profile incurs no plan management charge — that fee applies once a Dedicated profile,
environment private endpoint, or planned maintenance is in use. The Consumption free grant (180,000 vCPU-seconds and 360,000 GiB-seconds per subscription per
month) does not materially offset any of this: Phase3/002 already records that the free tier "is exhausted within minutes at hundreds of replicas" by the
Match Prediction Worker sharing the same subscription. Azure Files storage for any persistence attempt, the VNet and subnets, and the engineering time to
build and maintain a Redis image pipeline are all additional and are not costed here.

On the capacity question specifically, the concern that 2 vCPU / 4 GiB won't be enough is well founded as soon as the cache holds more than the
control-plane datasets. 4 GiB leaves roughly 3–3.5 GiB usable for Redis after container overhead, which covers the 200–500 MB-per-version HMD estimate with
room for two versions but leaves nothing for the "Could" candidates, and no margin if that estimate is low. The awkward part is that ACA's fixed 1:2
vCPU:memory ratio means buying memory headroom means buying vCPU a cache does not need: going from 4 GiB to 8 GiB doubles the vCPU bill as well, which is
why the 4 vCPU / 8 GiB row costs roughly double the 2 vCPU / 4 GiB row for memory that is the only thing actually wanted. AMR prices memory independently;
that is what the `Balanced` / `MemoryOptimized` tier distinction *is*.

Comparison at the sizing this workload actually calls for:

| Option | Monthly (uksouth, PAYG, GBP) | Usable for data | HA | SLA | What else |
|---|---|---|---|---|---|
| **AMR `Balanced_B3`, HA — recommended for production** | **~40** | ~2.4 GB | **Yes**, ≥2 nodes, zone-distributed | **Yes** | Patched, Entra auth, 15,000 connections, reachable by every consumer with no new networking |
| AMR `Balanced_B5`, HA | ~96 | ~4.8 GB | Yes | Yes | The step up if the HMD estimate proves low |
| AMR `Balanced_B10`, HA | ~195 | ~9.6 GB | Yes | Yes | 12 GB; `MemoryOptimized_M10` is the same 12 GB for ~133 with fewer vCPUs |
| AMR `Balanced_B1`, HA | ~20 | ~0.8 GB | Yes | Yes | Enough only while the cache holds control-plane data (ATL-194/195) |
| AMR `Balanced_B1`, non-HA (dev/uat) | ~10 | ~0.8 GB | No | No | Non-production only |
| Redis on ACA, 2 vCPU / 4 GiB | ~105 (47–163) | ~3–3.5 GB | **No** | **No** | Flushed on every restart and maintenance event; needs VNet + environment work first |
| Redis on ACA, 4 vCPU / 8 GiB | ~210 (95–326) | ~7 GB | **No** | **No** | Requires a workload profiles environment |
| Redis on ACA, Dedicated D4 node | ~285 | ~15 GB | **No** | **No** | No scale-to-zero; one node is still a single point of failure |
| Redis on ACA, HA topology (2 nodes as 2 apps) | 2× the above | — | Self-built | **No** | Sentinel/Cluster designed, deployed and operated by Atlas |

At every capacity point, Azure Managed Redis *with* high availability costs less than a single non-redundant Redis container on ACA:

| Usable capacity | AMR with HA | ACA single replica, realistic | AMR advantage |
|---|---|---|---|
| ~2.4–3.5 GB | `Balanced_B3` — **~£40** | 2 vCPU / 4 GiB — ~£105 | **2.6× cheaper, and HA** |
| ~4.8 GB | `Balanced_B5` — **~£96** | (between ACA rows) | — |
| ~7–9.6 GB | `Balanced_B10` — **~£195** | 4 vCPU / 8 GiB — ~£210 | **Cheaper, and HA** |
| ~15 GB | `MemoryOptimized_M10` (12 GB) — **~£133** | Dedicated D4 — ~£285 | **2.1× cheaper, and HA** |

The reason is structural rather than incidental: ACA Consumption memory works out at roughly £7.88 per GiB-month, but the fixed 1:2 vCPU:memory ratio drags
an active-rate vCPU bill (~£66 per vCPU-month) along with it, so 8 GiB of container memory costs £95–326/month. AMR B10 supplies 12 GB across two
replicated nodes — 24 GB of provisioned RAM — for £195. Self-hosting is only cheaper than a managed service when you can scale it to zero or pack it
alongside other workloads, and a cache can do neither.

Adding a private endpoint later adds a small per-endpoint hourly charge plus per-GB data processing to the AMR figures (order of £8/month at list rates —
confirm via the pricing calculator, as those meters are not exposed in the region query used above); that does not change the ranking.

### 5. Implementation readiness

The Terraform work is smaller than ATL-214 assumes, and its open spike can be closed now. The pinned provider, `hashicorp/azurerm` `= 4.74.0` per
`terraform/core/versions.tf`, already ships `azurerm_managed_redis`, which supersedes the deprecated `azurerm_redis_enterprise_cluster` /
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

Reachability is a solvable, priced work item, not a veto (§2), and a multi-replica Redis on ACA is not the free redundancy it might look like (§3a). Setting
those points aside entirely, the case for Azure Managed Redis rests on three things that survive removing every infrastructure constraint:

1. Cost runs the opposite way to the usual self-hosting intuition. At every capacity point, AMR *with* high availability is cheaper than a single
   non-redundant Redis container on ACA: ~£40/month versus ~£105/month at ~3 GB, and ~£195/month versus ~£210/month at ~8 GB. A cache cannot scale to zero
   and cannot share compute with another workload, so it forfeits both mechanisms that normally make self-hosting cheaper, while ACA's fixed 1:2
   vCPU:memory ratio forces the purchase of vCPU the workload has no use for. Building an actually-HA Redis topology doubles the ACA figure again.
2. High availability and an SLA are not something ACA can provide without Atlas building a Redis cluster by hand. ACA load-balances TCP across replicas,
   offers no session affinity for TCP, and replicates no application state, so multiple replicas of one app are multiple independent caches. A genuine
   topology is constructible as one container app per Redis node with Sentinel or Cluster, but that is a self-managed Redis cluster on a platform that
   restarts replicas on its own schedule and whose health probes know nothing about Redis replication state.
3. Operational ownership is what Phase3/001 weighted highest for exactly this team: mirroring and patching a third-party Redis image, owning `redis.conf`
   and `maxmemory` policy, building Redis metrics that AMR exposes natively, and holding a password secret where AMR uses managed identity.

Reachability reinforces rather than drives that conclusion, via one asymmetry: because external TCP ingress is a documented hard requirement for a VNet,
the "reuse infrastructure we already own" option is the one needing net-new networking (a VNet, subnets, a rebuilt or additional ACA environment, and VNet
integration across the Function Apps), while AMR needs none of it at this phase. If Atlas builds that VNet for other reasons, AMR simply gains a private
endpoint from it; the work is not wasted, and it is not a prerequisite.

### Provisioning

1. Resource: `azurerm_managed_redis` in `terraform/core`, on the pinned `azurerm 4.74.0` provider. No provider upgrade required.
2. SKU: `Balanced`, not `MemoryOptimized` or `ComputeOptimized`. The profile is a small number of large values read at low frequency: there is no
   throughput case for Compute Optimized, and Memory Optimized starts at 12 GB (~£133/month HA), far more memory than needed. `Balanced` starts at 0.5 GB
   and covers the whole range Atlas will plausibly want.
   - Production (live, wmda-live): `Balanced_B3` with `high_availability_enabled = true`. ~£40/month. 3 GB (~2.4 GB usable after AMR's ~20% system
     reservation) gives comfortable headroom for two nomenclature versions of HMD data coexisting across a Data Refresh rollover, on the 200–500 MB
     per-version estimate. Its 15,000-connection limit is far above the low hundreds this fleet will open.
   - Non-production (dev, uat): `Balanced_B1` with `high_availability_enabled = false`. ~£10/month. Non-HA is appropriate here given those instances
     carry no SLA and may lose data during maintenance, and dev/uat need not hold a full production-scale HMD dataset.
   - If ATL-194 ships before ATL-196, `Balanced_B1` with HA (~£20/month) is sufficient for production in the interim, since the cache then holds only
     sub-megabyte control-plane data. Scale up to B3 before ATL-196 goes live.
   - Revisit the size on measured `Used Memory` once HMD data is actually resident; the 200–500 MB figure is an estimate, and memory size and
     performance tier are both in-place scale operations. Note that `high_availability_enabled` is *not* an in-place change, it forces recreation, so
     the HA choice must be right per environment at creation time.
3. Authentication: Microsoft Entra ID via managed identity. Leave `access_keys_authentication_enabled` at its `false` default and grant access with
   `azurerm_managed_redis_access_policy_assignment`. No connection-string secret in `local.settings.template.json` or the container app's `secret` blocks.
4. Transport: leave `client_protocol` at `"Encrypted"`.
5. Clustering: leave `clustering_policy` at `"OSSCluster"`. `StackExchange.Redis` needs no special configuration for it.
6. Persistence: none. Every cached value is reconstructible from SQL or Table Storage, and the design already requires graceful degradation to the
   source. Persistence would add cost and operational surface for no correctness gain.
7. Eviction policy: set `eviction_policy = "AllKeysLRU"` explicitly rather than accepting the `VolatileLRU` default. `VolatileLRU` only ever evicts keys
   that carry a TTL. v2's design does give HMD entries a 12-hour L2 lifetime, so in the intended steady state most keys would be evictable under either
   policy, but the default makes correct behaviour under memory pressure contingent on every writer remembering to set a TTL, and a full cache holding
   any TTL-less entries returns write errors instead of shedding cold data. `AllKeysLRU` costs nothing, removes that contingency, and is the right policy
   for a store that is by design a pure cache with an authoritative source behind it. Set it deliberately; don't inherit it.

### Network posture — phased, deliberately

8. Phase 1 (now): `public_network_access = "Enabled"`, with TLS and Entra-only authentication. This is not a compromise on the existing Atlas security
   baseline. Azure SQL is already reached through an `0.0.0.0` "Allow Azure services" firewall rule and Storage over public endpoints with shared keys, and
   this is an improvement on that, since the cache uses managed identity rather than a secret. It unblocks ATL-190 and ATL-194 (which fixes a live
   correctness defect) without waiting on a networking programme.
9. Phase 2 (whenever the VNet workstream happens, not before): private endpoint with `public_network_access = "Disabled"`. This requires a VNet, a
   private DNS zone, and regional VNet integration for the Elastic Premium apps; for the Match Prediction Worker it also requires the ACA environment to be
   recreated with VNet integration, since network type is immutable after creation. ATL-214's private-endpoint acceptance criterion should move out of
   that ticket into this workstream, rather than blocking the cache foundation on networking Atlas has never had and, per the team, does not require at
   this phase. Two notes on sequencing: this is the *same* networking work a self-hosted Redis on ACA would have needed just to be reachable, so choosing
   AMR defers it rather than avoiding it; and it is worth doing on its own merits eventually, since it would also close the `0.0.0.0` SQL firewall rule. It
   remains a networking decision, not a caching one.

### Consumer-side constraints this decision imposes

10. Keep large HMD values out of the hot read path. ATL-196 is explicit that L2 is consulted on cold start and miss only, with L1 serving steady-state
    hot reads. Multi-megabyte serialised tables must not be fetched per lookup: that is the difference between this decision paying for itself and it
    making things slower.
11. Do not put a per-operation Redis round trip on the `ScoringCache` hot path, if that candidate is ever scoped. Its values are a few bytes behind a
    ~70-byte key and it is called per locus per donor per search; a network hop per lookup would cost more than the calculation it caches. If it is
    distributed at all, reads must be batched (pipelined or `MGET`) at a natural boundary, with L1 serving steady state.
12. Protect the sources of truth against a fallback storm. A cold or unavailable L2 coinciding with a 0→50-replica burst turns into 50 replicas
    simultaneously re-crawling Table Storage and SQL. Bounded concurrency or a single-flight guard on the fallback path is required. v2 names this
    explicitly, and it becomes load-bearing the moment L2 exists.
13. ATL-305 (bounding `HaplotypeFrequencyCache`) is not superseded by any of this. L2 solves staleness and redundant loads; it does not bound a
    replica's own memory. That ticket should still land before the ATL-233 precompute consumer reaches production.
14. ATL-197 (MAC Dictionary L2) is out of scope per v2 ("not needed in L2") and should be closed or parked rather than built against this cache. Nothing
    in this hosting decision depends on it, and its ~567,000 entries are the largest single dataset that will *not* be sized for.

### Revisit if

- Measured `Used Memory` exceeds a `Balanced_B10`/`MemoryOptimized_M10` (12 GB) and cost becomes material — at which point Flash Optimized tiers, or
  reservations (1- and 3-year terms are available for AMR), are the next levers, not self-hosting.
- A compliance requirement mandates single-tenant compute or private-only networking. Note that this argues for AMR *with a private endpoint*, not for
  self-hosting: an ACA container is also multi-tenant compute on the Consumption profile.
- Atlas acquires a VNet-integrated workload profiles Container Apps environment for other reasons. That removes the reachability and capacity objections to
  self-hosting, and is the scenario in which this ADR is most worth re-reading — but the cost comparison above already assumes that environment exists, and
  AMR still wins it. The HA, SLA and operational-ownership objections are unaffected.
- Atlas needs Redis for something that is *not* a cache — a durable queue, a lock service, a system of record — where data loss on restart stops being
  acceptable. That would change the persistence and HA requirements in this ADR's working profile substantially, and should be its own decision.

## Consequences

**Easier:**

- Every L2 consumer, on either compute platform, can share one cache with no networking prerequisite at all, so ATL-196, the "Must" consumer, becomes
  deliverable immediately rather than after a VNet, an environment rebuild, and VNet integration across the Function App fleet.
- ATL-194's live correctness defect, replicas serving a stale active-set list after an import, is fixable now, without waiting on any networking work. A
  naively-replicated self-hosted Redis would also have *reintroduced* this defect, since ACA load-balances TCP across replicas with no affinity option and
  no state replication between them.
- The VNet workstream stays optional and independently prioritised, rather than becoming a blocker on the caching epic. If and when it happens, AMR gains a
  private endpoint from it.
- ATL-214's open spike is closed: the resource is `azurerm_managed_redis`, available on the currently pinned provider, and its defaults already satisfy the
  ticket's Entra-ID/no-access-keys requirement.
- No new Redis operational surface: patching, version upgrades, node replacement, failover and zone distribution are Microsoft's responsibility — consistent
  with the reasoning that chose ACA over AKS in Phase3/001.
- Cost is low, predictable, and scales with a supported in-place operation rather than a re-provisioning: ~£10/month per non-production environment and
  ~£40/month for production, with memory size adjustable on measured usage.
- The Container Apps environment is untouched. The team has confirmed the `prevent_destroy` lock could be removed and a new environment created if needed —
  this decision simply means neither is necessary for caching, and the legacy Consumption-only environment can be migrated on its own merits and timeline
  rather than as a caching prerequisite. (Migrating it is still worth doing eventually: Microsoft describes that environment type as legacy, and it caps
  replicas at 2 vCPU / 4 GiB, which constrains the Match Prediction Worker itself — see ATL-305.)

**More difficult / risks:**

| Risk | Severity | Mitigation |
|---|---|---|
| The cache is publicly addressable in Phase 1, protected by TLS and Entra ID but with no IP-level restriction (AMR supports neither VNet injection nor IP firewall rules) | **Low** — consistent with the team's position that Atlas infrastructure remains publicly reachable at this phase | Entra-only auth with access keys disabled; enable connection audit logs via diagnostic settings; adopt the private endpoint whenever the VNet workstream lands. Confirm at provisioning whether the portal Firewall blade offers usable IP rules, as documentation is inconsistent on this point. Note the alternative was *worse*: an ACA Redis reachable across environments would be published on a public TCP port behind only a Redis `AUTH` password |
| A new paid Azure resource requires cost sign-off before provisioning | **Medium** | Figures above are from the Azure Retail Prices API for uksouth; the production line item is ~£40/month (`Balanced_B3`, HA) and ~£10/month per non-production environment (`Balanced_B1`, non-HA). ATL-214 already carries the sign-off acceptance criterion |
| The 200–500 MB HMD-per-version estimate is unmeasured, so `Balanced_B3` could prove under- or over-sized | **Medium** | Memory size is an in-place scale operation in both directions (with documented restrictions on scaling down), so this is recoverable. Instrument `Used Memory` from the first ATL-196 deployment and right-size before HMD goes live in production |
| `eviction_policy` left at the `VolatileLRU` default causes write failures on a full cache under versioned keys | **Medium** | Set `AllKeysLRU` explicitly in Terraform and assert it in the review of ATL-214's PR |
| A cold or unavailable L2 during a 50-replica burst produces a fallback storm against SQL/Table Storage | **Medium** | Bounded-concurrency or single-flight guard on the fallback path (design decision D3), specified as part of ATL-190's abstraction rather than left to each consumer |
| Failover or maintenance produces a 10–15 s window of transient faults | **Low** | Cache-aside with retry and circuit-breaker on the client; a miss is a slow read, not a failed search. Scheduled maintenance windows (preview) can further constrain when this happens |
| Non-HA dev/uat instances can lose data during maintenance and carry no SLA | **Low** | Accepted deliberately for non-production; production runs HA. Note that `high_availability_enabled` cannot be toggled in place — it forces recreation — so the per-environment choice must be made at creation |
| Integration tests need a Redis, and the repo has no local Redis provision | **Low** | Already flagged on ATL-194. A container-based Redis for local/CI integration testing is a legitimate and unrelated use of a Redis container — this ADR rejects self-hosting for the *production cache tier*, not for test fixtures |
| Regional availability must be confirmed for both `uksouth` and `westeurope` (WMDA installations) before provisioning | **Low** | Check AMR availability and the specific SKU's status per region; note that Balanced sizes above 350 GB are in preview, which is far above anything proposed here |
