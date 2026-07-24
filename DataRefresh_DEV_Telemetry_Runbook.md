# Data Refresh — DEV Telemetry Runbook (ATL-216)

Companion to `DataRefresh_PerformanceSpike_Plan.md`. This branch (`atl-216-data-refresh-telemetry`)
carries **only** the telemetry + robustness changes needed to measure Data Refresh **stages 40
(DonorImport) and 50 (DonorHlaProcessing)** at prod scale on DEV — no profiling harness, no local
Service Bus emulator, no `local.settings.json`/secret changes from the spike branch.

The question we are answering on DEV (44M-donor prod copy, prod performance tiers): **is stage 40 / 50
bound by our CPU or by the database?** — and, specifically, does the profiled **Finding #1** (the
per-batch full-table re-cache of HLA names & p-groups is ~55% of stage-50 user code) hold at prod scale.

---

## 1. What this branch instruments

All durations are emitted as **pre-aggregated App Insights metrics** — metric name
`DataRefresh.DurationMs`, dimensions `Operation` and `Locus`. Pre-aggregated metrics are **never
sampled** (unlike the old `TrackTrace` timers, which the isolated worker's adaptive sampling dropped in
bursts at stage boundaries — the reason the historical intra-stage split was unrecoverable).

Worker sampling is also reconfigured (`Atlas.MatchingAlgorithm.Functions/Program.cs`): adaptive sampling
is re-added **excluding `Trace;Exception;Event`**, so progress traces and (importantly) all exception
telemetry survive; only the very-high-volume SQL dependency telemetry is sampled.

### Operation taxonomy (`customDimensions.Operation`)

| Operation | Stage | Kind | Notes |
|---|---|---|---|
| `DonorImportStageTotal` | 40 | **total** | whole stage 40. Cross-DB donor **stream read** ≈ this − Σ`DonorImportBatch` |
| `DonorImportBatch` | 40 | parent | per 10k-donor batch |
| `DonorInfoConversion` | 40 | **CPU** | per-donor conversion loop |
| `DonorBulkInsert` | 40 | **DB** | `SqlBulkCopy` → `Donors` |
| `DonorManagementLogWrite` | 40 | **DB** | mgmt-log write (only when `ShouldMarkAllDonorsAsUpdated`, which the refresh sets true) |
| `HlaProcessingStageTotal` | 50 | **total** | whole stage 50 |
| `BatchProcessing` | 50 | parent | per 2k-donor batch = `HlaExpansion` + `ImportHlaOverall` + `UpsertOverall` |
| `HlaExpansion` | 50 | **CPU** | HMD expansion off the pre-warmed cache (Phase B: ~0.9%) |
| `ImportHlaOverall` | 50 | parent | **the slice that was previously unmeasured** — decomposed by the 5 rows below |
| `EnsureProcessedHlaCache` | 50 | **DB** (read) | first-batch full per-locus read of existing relations |
| `EnsurePGroupsExist` | 50 | **DB** (read+write) | p-group insert **+ full-table re-cache** — Finding #1 |
| `EnsureHlaNamesExist` | 50 | **DB** (read+write) | HLA-name insert **+ full-table re-cache** — Finding #1 (the ~55%) |
| `BuildHlaRelations` | 50 | **CPU** | relation build + `PhenotypeInfo`/`LociInfo` allocations (Finding #3) |
| `InsertHlaRelations` | 50 | **DB** (write) | `SqlBulkCopy` → `HlaNamePGroupRelation*` |
| `UpsertOverall` | 50 | parent | the per-locus `MatchingHlaAt*` upsert, all loci |
| `BulkInsertSetup` | 50 | mixed (per `Locus`) | starts the per-locus task; the synchronous DataTable build runs here |
| `BuildDataTable` | 50 | **CPU** (per `Locus`) | `MatchingHlaAt*` DataTable build |
| `DeleteExistingRecords` | 50 | **DB** (per `Locus`) | ≈0 during a refresh (`isKnownToBeCreate`) |
| `BlockingWaitOnDbInsert` | 50 | **DB wall-clock** | wait on the parallel bulk copies (`Locus=all` = the `WhenAll`; per-`Locus` = transactional path only) |
| `DbBulkInsert` | 50 | **DB** (per `Locus`) | `SqlBulkCopy` → `MatchingHlaAt*` — **summed across the 5 parallel locus threads** |

> **Double-counting caveats when summing.** `ImportHlaOverall`, `BatchProcessing`, `UpsertOverall`,
> `HlaProcessingStageTotal`, `DonorImportBatch`, `DonorImportStageTotal` are **parents** — never sum them
> with their children. `BulkInsertSetup` overlaps `BuildDataTable`; `BlockingWaitOnDbInsert` (wall-clock
> wait) overlaps `DbBulkInsert` (summed across parallel threads). For a work-total comparison use the
> **leaves** listed in §4; for a wall-clock DB figure prefer `BlockingWaitOnDbInsert` with `Locus == "all"`.

### Robustness (exception surfacing)

Stage failures previously surfaced only as low-severity/`$`-typo'd `Trace` text that never reached the
queryable `exceptions` table. Now:

- **`DataRefreshRunner`** wraps the whole stage loop and emits `SendException` on failure with
  `customDimensions`: `DataRefreshStage` (which stage died), `DataRefreshRecordId`, `Disposition`.
  `SqlException` → **Error** (the designed "rethrow → Service Bus redelivery → resume from checkpoint"
  path) so retries don't cry wolf; anything else → **Critical** (terminal). Teardown + rethrow unchanged.
- **`DataRefreshOrchestrator`** — both catch blocks now emit `SendException` (with `DataRefreshRecordId` +
  `Disposition`) instead of `Trace`; retry/notify/mark-failed behaviour unchanged.
- **`DonorImporter`** (stage 40) — emits `SendException` (full stack) before wrapping/rethrowing.

Exceptions are never sampled (excluded above), and Critical always passes any configured log level, so a
stage failure will always be visible. **Behaviour/control-flow is unchanged — only observability.**

---

## 2. Deploy to DEV

1. Check out and deploy this branch to the DEV **Matching Algorithm Functions** app (the app that hosts
   `RunDataRefresh`) via the normal DEV pipeline / `func azure functionapp publish` / Rider publish.
2. Metrics need **no** log-level change (`SendMetric` is not log-level gated). To also capture the
   human-readable **progress/ETA** traces and the Verbose donor-import batch traces, ensure the app's
   `ApplicationInsights:LogLevel` is `Info` (progress) or `Verbose` (import batches). Exceptions & metrics
   come through regardless.
3. Confirm the DEV Service Bus has the `DataRefresh` requests topic/subscription provisioned (it triggers
   `RunDataRefresh`).

---

## 3. Run a Data Refresh on DEV

The refresh runs against the **dormant** transient DB (A or B — whichever is *not* currently serving
search), then hot-swaps on success. On the 44M copy expect **~12 h** wall-clock.

> ⚠️ This is destructive to the **dormant** DB (it scales it up, TRUNCATEs, re-imports and re-processes
> all donors, rebuilds indexes, scales down) and consumes the higher DB tier for the duration. It does
> **not** touch the currently-active DB until the final successful hot-swap. This deliberately overrides
> the spike plan's "don't trigger a refresh on DEV" note — that's the whole point of this branch.

### Trigger (manual HTTP)

`SubmitDataRefreshRequestManual` is an `AuthorizationLevel.Function` HTTP POST.

```bash
# 1. Find the app + a function key
az functionapp list -g <dev-resource-group> --query "[?contains(name,'matching')].name" -o tsv
FUNC_KEY=$(az functionapp keys list -g <dev-resource-group> -n <dev-matching-functions-app> \
             --query functionKeys.default -o tsv)

# 2. Fire it. forceDataRefresh:true is REQUIRED here — otherwise, if DEV's active HMD is already on the
#    latest WMDA nomenclature, a manual request is rejected ("...not run in 'Forced' mode").
curl -s -X POST \
  "https://<dev-matching-functions-app>.azurewebsites.net/api/SubmitDataRefreshRequestManual?code=$FUNC_KEY" \
  -H "Content-Type: application/json" \
  -d '{"forceDataRefresh": true}'
# -> {"dataRefreshRecordId": <id>, ...}
```

The POST validates, writes a `DataRefreshHistory` record against the dormant DB, and publishes to the
Service Bus requests topic; `RunDataRefresh` then executes the 9 stages (`functionTimeout` is 2 days, so
it will not be killed mid-run).

### Before you run / gotchas

- **One at a time.** If an incomplete refresh record exists, the request is rejected
  (`"Data refresh seems to already be in progress"`). Check with the SQL in §4, and if a previous run is
  genuinely dead (not just resuming), clean up via the `RunDataRefreshCleanup` HTTP function or by
  resolving the open `DataRefreshHistory` row.
- **Resume vs restart.** A `SqlException` / host restart mid-run is *resumed* automatically via Service
  Bus redelivery from the per-stage checkpoint — you do **not** re-POST. A new POST is only for a fresh run.
- **Which DB.** The run targets the dormant DB; don't assume A vs B — the record's `Database` column tells
  you which one this run used.

---

## 4. Kusto / SQL query pack

App Insights = the resource wired to the DEV Matching Algorithm Functions app. Narrow `ago(...)` to your
run window once you have the record timestamps from the SQL below. (These supersede the plan's A2/A3
`traces`-parsing queries, which targeted the **old** text-based timers that no longer exist.)

### 4.0 Sanity — did the metrics land? (master breakdown)

```kusto
customMetrics
| where name == "DataRefresh.DurationMs" and timestamp > ago(2d)
| extend Operation = tostring(customDimensions.Operation), Locus = tostring(customDimensions.Locus)
| summarize totalMin = round(sum(valueSum)/60000.0, 1), calls = sum(valueCount),
            avgMs = round(sum(valueSum)/sum(valueCount), 1)
    by Operation, Locus
| order by totalMin desc
```

### 4.1 Stage ranking (SQL `DataRefreshHistory` — durable, never sampled)

```sql
-- Recent runs + the per-stage completion timestamps.
SELECT TOP 10 Id, [Database], HlaNomenclatureVersion, WasSuccessful, RefreshAttemptedCount,
       RefreshRequestedUtc, RefreshLastContinuedUtc, RefreshEndUtc,
       DonorImportCompleted, DonorHlaProcessingCompleted
FROM MatchingAlgorithmPersistent.DataRefreshHistory
ORDER BY Id DESC;

-- Per-stage minutes for one clean run (prefer RefreshAttemptedCount = 1).
DECLARE @RecordId INT = (SELECT MAX(Id) FROM MatchingAlgorithmPersistent.DataRefreshHistory WHERE WasSuccessful = 1);
SELECT s.Ord, s.StageName, DATEDIFF(SECOND, s.StageStart, s.StageEnd) / 60.0 AS DurationMinutes
FROM MatchingAlgorithmPersistent.DataRefreshHistory r
CROSS APPLY (VALUES
    (0,  '0  MetadataDictionaryRefresh', COALESCE(r.RefreshLastContinuedUtc, r.RefreshRequestedUtc), r.MetadataDictionaryRefreshCompleted),
    (10, '10 IndexRemoval',              r.MetadataDictionaryRefreshCompleted, r.IndexDeletionCompleted),
    (20, '20 DataDeletion',              r.IndexDeletionCompleted,             r.DataDeletionCompleted),
    (30, '30 DatabaseScalingSetup',      r.DataDeletionCompleted,              r.DatabaseScalingSetupCompleted),
    (40, '40 DonorImport',               r.DatabaseScalingSetupCompleted,      r.DonorImportCompleted),
    (50, '50 DonorHlaProcessing',        r.DonorImportCompleted,               r.DonorHlaProcessingCompleted),
    (60, '60 IndexRecreation',           r.DonorHlaProcessingCompleted,        r.IndexRecreationCompleted),
    (70, '70 DatabaseScalingTearDown',   r.IndexRecreationCompleted,           r.DatabaseScalingTearDownCompleted),
    (80, '80 QueuedDonorUpdates',        r.DatabaseScalingTearDownCompleted,   r.QueuedDonorUpdatesCompleted)
) AS s(Ord, StageName, StageStart, StageEnd)
WHERE r.Id = @RecordId
ORDER BY s.Ord;
```

### 4.2 CPU-vs-DB verdict — stage 50 (the decisive read)

Buckets the **non-overlapping leaves** only (parents + the overlapping `BulkInsertSetup` /
`BlockingWaitOnDbInsert` are excluded — see the caveat in §1).

```kusto
customMetrics
| where name == "DataRefresh.DurationMs" and timestamp > ago(2d)
| extend Operation = tostring(customDimensions.Operation)
| where Operation in ("HlaExpansion", "BuildHlaRelations", "BuildDataTable",                      // CPU leaves
                      "EnsureProcessedHlaCache", "EnsurePGroupsExist", "EnsureHlaNamesExist",     // DB leaves
                      "InsertHlaRelations", "DeleteExistingRecords", "DbBulkInsert")
| extend Kind = iff(Operation in ("HlaExpansion", "BuildHlaRelations", "BuildDataTable"), "CPU (our code)", "DB (SQL)")
| summarize totalMin = round(sum(valueSum)/60000.0, 1) by Kind, Operation
| order by Kind asc, totalMin desc
```

> `DbBulkInsert` is summed across the 5 parallel locus threads, so DB "work" is inflated vs wall-clock;
> for the wall-clock DB-write figure use `BlockingWaitOnDbInsert` where `Locus == "all"`.

### 4.3 Finding #1 — is `ImportHla` (the name/p-group re-cache) the stage-50 hotspot?

```kusto
customMetrics
| where name == "DataRefresh.DurationMs" and timestamp > ago(2d)
| extend Operation = tostring(customDimensions.Operation)
| where Operation in ("BatchProcessing", "HlaExpansion", "ImportHlaOverall", "UpsertOverall",
                      "EnsureProcessedHlaCache", "EnsurePGroupsExist", "EnsureHlaNamesExist",
                      "BuildHlaRelations", "InsertHlaRelations")
| summarize totalMin = round(sum(valueSum)/60000.0, 1), calls = sum(valueCount) by Operation
| extend PctOfBatch = round(100.0 * totalMin / toscalar(
    customMetrics
    | where name == "DataRefresh.DurationMs" and timestamp > ago(2d)
    | where tostring(customDimensions.Operation) == "BatchProcessing"
    | summarize sum(valueSum)/60000.0), 1)
| order by totalMin desc
// Expect EnsureHlaNamesExist (+EnsurePGroupsExist) to dominate ImportHlaOverall if Finding #1 holds at prod scale.
```

Independent corroboration from auto-collected SQL dependency telemetry (the full-table re-cache SELECTs):

```kusto
dependencies
| where timestamp > ago(2d) and type == "SQL"
| where data has "FROM HlaNames" or data has "FROM PGroupNames" or data has_cs "HlaNamePGroupRelation"
| summarize calls = count(), totalMin = round(sum(duration)/60000.0, 1), avgMs = round(avg(duration), 1) by data
| order by totalMin desc
```

### 4.4 CPU-vs-DB verdict — stage 40 (DonorImport)

```kusto
customMetrics
| where name == "DataRefresh.DurationMs" and timestamp > ago(2d)
| extend Operation = tostring(customDimensions.Operation)
| where Operation in ("DonorImportStageTotal", "DonorInfoConversion", "DonorBulkInsert", "DonorManagementLogWrite")
| summarize totalMin = round(sum(valueSum)/60000.0, 1), calls = sum(valueCount) by Operation
| order by totalMin desc
// CPU = DonorInfoConversion; DB = DonorBulkInsert + DonorManagementLogWrite;
// cross-DB donor stream read ≈ DonorImportStageTotal - (DonorInfoConversion + DonorBulkInsert + DonorManagementLogWrite).
```

### 4.5 Robustness — did any stage throw? (surfaced exceptions)

```kusto
exceptions
| where timestamp > ago(7d)
| extend Stage = tostring(customDimensions.DataRefreshStage),
         Disposition = tostring(customDimensions.Disposition),
         RecordId = tostring(customDimensions.DataRefreshRecordId)
| where isnotempty(RecordId) or isnotempty(Stage)
| project timestamp, severityLevel, Stage, Disposition, RecordId, type, outerMessage, method = operation_Name
| order by timestamp desc
// severityLevel 3 = Error (transient SqlException, will resume); 4 = Critical (terminal).
// Broader fallback if the dims are missing: exceptions | where cloud_RoleName has "matching" and severityLevel >= 3
```

### 4.6 Live progress / ETA while a run is in flight

```kusto
traces
| where timestamp > ago(1d) and message has "HLA Processing progress"
| project timestamp, message
| order by timestamp desc
// "HLA Processing progress: <done>/<total> batches (NN.N%). Projected completion: <utc>."
```

---

## 5. Reconcile

1. §4.1 stage-50 minutes should ≈ `HlaProcessingStageTotal` from §4.0; §4.1 stage-40 minutes ≈
   `DonorImportStageTotal`. Cross-checking the durable SQL against the metrics validates both substrates.
2. §4.2 gives the headline CPU-vs-DB split; §4.3 attributes the DB share (if DB-bound) to the name/p-group
   re-cache and confirms/refutes Finding #1 at prod scale — corroborated by the §4.3 `dependencies` counts.
3. Dev-scale caveat is gone here (this is the 44M prod copy on prod tiers), but the parallel-thread
   summing caveat (§1) still applies when reading `DbBulkInsert`.
