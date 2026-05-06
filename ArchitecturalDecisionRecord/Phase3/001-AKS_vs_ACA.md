# AKS vs ACA for Function App Containerization

## Status

Accepted.

## Context

The project is migrating up to 10 Azure Function Apps from the **Elastic Premium (EP) plan** to containers. The EP plan hits concurrency and memory limits under donor scoring matching load, resulting in unreasonable cost and processing slowdowns. The billing model is misaligned with bursty, high-memory workloads.

**Project drivers:**

- Processing time optimization — donor scoring exceeds EP plan limits
- Cloud cost optimization — EP billing inefficient for burst workloads
- Elastic scalability — KEDA-driven scale-out/in from zero via Azure Service Bus queue depth
- Compute flexibility — GPU and memory-optimized tiers required for scoring workloads

**Constraints:**

- Up to 10 containerized Azure Function Apps
- Single DevOps engineer with limited capacity taking ownership post-delivery
- Must integrate with existing Azure DevOps CI/CD pipelines
- Existing observability stack: Log Analytics + Application Insights + Azure Monitor
- HTTPS connectivity required between all services (app-to-app, Blob Storage, Azure SQL)
- Long-term solution — platform must scale with the product

## Investigation

Seven factors were evaluated and weighted against project drivers and constraints.

| Factor | Weight | AKS | ACA (Dedicated Plan) |
|---|---|---|---|
| Ops burden (1 DevOps, limited capacity) | **High** | Cluster lifecycle, node pool ops, upgrades — ongoing ownership required | Fully managed control plane — no cluster ownership, minimal daily ops |
| Compute tiers (GPU, memory-optimized) | **High** | Full Azure VM catalog, any SKU, dedicated GPU node pools | E4–E32 mem-optimized + A100/T4 GPU tiers — satisfies workload requirement |
| KEDA / Service Bus scaler | **High** | Native KEDA — full control, ScaledJob + ScaledObject, custom scalers | Managed KEDA — ASB scaler supported, less granular tuning available |
| Observability (Log Analytics + App Insights + Monitor) | **Medium** | Manual wiring — OTel collector, Prometheus, Monitor agent DaemonSet | Native — Log Analytics workspace, App Insights, Azure Monitor built-in |
| CI/CD (Azure DevOps pipelines) | **Medium** | Helm + kubectl tasks — more moving parts | `AzureContainerApps@1` native ADO task — simpler pipeline surface |
| TLS / cert management | **Medium** | cert-manager + ingress — owned by the DevOps engineer | Managed TLS — auto-issued, auto-rotated per app; BYOC supported |
| Cost / scale-to-zero | **Medium** | Node-based billing — idle cost regardless of utilization | Consumption + dedicated mix — scale-to-zero by default, pay-per-use |
| Long-term extensibility | **Low** | Unconstrained — service mesh, custom operators, full K8s ecosystem | Opinionated platform — KEDA tuning ceiling, Dapr-centric extensions |

**Summary: ACA wins 5/7 factors. AKS wins 2/7 (KEDA granularity, long-term extensibility ceiling).**

**Key findings per factor:**

1. **Operational burden** — ACA transfers all control plane responsibility to Microsoft. The single DevOps engineer's scope reduces to application config, scaling rules, env vars, and pipeline definitions.

2. **Compute tiers** — ACA Dedicated plan offers memory-optimized profiles (E4–E32, up to 256 GB RAM) and GPU tiers (T4 on consumption, A100 on dedicated), satisfying scoring workload requirements without node pool management.

3. **KEDA / ASB scaler** — ACA runs a managed KEDA implementation. ACA Jobs (run-to-completion semantics) cover the batch scoring pattern. Risk: advanced tuning (custom cooldown, activation thresholds, multi-scaler composition) is limited. **Action required: prototype ASB scaler against real queue burst profile before committing.**

4. **Observability** — ACA integrates natively with the existing stack. Log Analytics is a first-class environment property; App Insights wired via connection string; Azure Monitor metrics available without DaemonSet. AKS requires OTel Collector, Container Insights wiring, and Monitor agent DaemonSet — an entire additional implementation workstream.

5. **CI/CD** — ACA maps the existing Function App pipeline directly to `AzureContainerApps@1`. AKS adds Helm chart management, `kubectl apply` tasks, and `values.yaml` parameterization. Both are maintainable; ACA introduces less new tooling surface.

6. **TLS** — ACA provides fully managed TLS at environment ingress level with automatic issuance and rotation. AKS requires cert-manager, ClusterIssuer resources, ingress controller integration, and rotation monitoring — all owned by a single engineer.

7. **Cost / scale-to-zero** — AKS node-based billing incurs idle cost regardless of utilization; achieving true scale-to-zero requires Karpenter or CAST AI. ACA consumption plan scales to zero by default; dedicated plan provides a predictable cost floor. Consumption + dedicated mix enables precise cost allocation per workload type.

## Decision

**Adopt Azure Container Apps on the Dedicated plan.**

- Use **memory-optimized workload profiles (E8/E16)** for donor scoring functions
- Use **Consumption plan** for lightweight and infrequent functions
- Validate KEDA Service Bus scaler behavior against the scoring queue burst profile **before finalizing the architecture**
- Proceed with **AKS only if managed KEDA proves insufficient** for the workload

**Recommended architecture:**

- Single shared ACA Environment, dedicated VNET, private endpoints to Azure SQL and Blob Storage
- ACA Jobs (run-to-completion) for donor scoring — triggered by ASB queue depth
- ACA Apps (persistent) for HTTP endpoints or long-running processing
- Min replicas: 0; max replicas: sized to queue drain SLA
- Workload Identity for Service Bus `TriggerAuthentication` — no connection string secrets
- Log Analytics at ACA environment level, App Insights via env var, Azure Monitor without additional agents
- GPU workload profile (NC8as-T4 or NC24-A100) added on-demand if model inference requires it

**Migration path:**

- Phase 1: Containerize functions using Azure Functions base images — no code changes required
- Phase 2: Deploy to ACA alongside existing EP plan; validate scaling behavior under load
- Phase 3: Migrate traffic function-by-function; decommission EP plan post-validation

## Consequences

**Easier:**

- Operational ownership for a single DevOps engineer — no cluster lifecycle management
- Native observability integration — eliminates an entire implementation workstream
- CI/CD pipeline simplification — `AzureContainerApps@1` replaces Helm + kubectl surface
- TLS management eliminated — auto-issued, auto-rotated certificates
- Cost efficiency — scale-to-zero by default, pay-per-use consumption model

**More difficult / risks:**

| Risk | Severity | Mitigation |
|---|---|---|
| KEDA managed scaler insufficient for burst profile | **High** | Prototype ASB scaler against real queue data before committing. Fallback: AKS if custom scaler tuning proves essential. |
| Single DevOps knowledge gap on ACA | **Medium** | ACA is significantly simpler than AKS. Document environment config and scaling rules during handover. |
| Workload profile sizing incorrect | **Medium** | Benchmark existing EP function memory ceiling before selecting E-series tier. Start with E8, scale based on observed utilization. |
| GPU tier availability in target region | **Low** | Verify NC-series workload profile availability in the target Azure region before commitment. |
| ACA platform ceiling in 18–24 months | **Low** | If requirements grow toward service mesh, custom operators, or advanced network policy — reassess AKS. Migration from ACA to AKS is feasible at that point. |
