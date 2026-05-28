---
name: azurerm-upgrade
description: >
  Assists with migrating a Terraform codebase between any two versions of the
  azurerm provider (e.g. v3→v4, minor bumps, or patch upgrades). Use this skill
  whenever the user mentions upgrading, migrating, or bumping the azurerm
  Terraform provider — including phrases like "upgrade azurerm", "migrate to
  azurerm v4", "bump provider version", "terraform provider upgrade azure", or
  "breaking changes azurerm". Also trigger when the user asks about azurerm
  changelog analysis, state surgery after a provider bump, or environment
  rollout plans for a Terraform provider change. Covers major, minor, and patch
  version transitions.
---

# AzureRM Provider Upgrade Skill

Guide an experienced Terraform / Azure engineer through a structured, phased
migration of their azurerm provider from **{SOURCE_VERSION}** to
**{TARGET_VERSION}**.

> **First step every time:** ask the user for the source and target provider
> versions if they haven't stated them. Pin both values and use them
> consistently throughout all phases.

## Context assumptions

- The engineer is experienced with Terraform and Azure.
- The codebase uses Terraform workspaces for environment separation
  (atlas-dev, atlas-uat, atlas-production by default).
- Environment-specific configuration lives in per-environment `.tfvars` files.
- All `terraform apply` runs execute through a CI/CD pipeline under an
  elevated Service Principal — no manual applies.
- The codebase may contain root and child modules with remote backends.
- Operate in a consultancy context: be precise, structured, and concise.

---

## Phase 1 — Codebase Analysis

Before proposing any changes, scan the full Terraform codebase and produce a
structured inventory.

Steps:

1. List every `azurerm_*` resource type and data source in use.
2. For each, record the file path (relative to repo root) and approximate
   usage count.
3. Identify all provider version constraints across root and child modules
   (`required_providers`, `version = "~> X.Y"`, etc.).
4. Flag deprecated arguments, removed resources, or renamed attributes
   affected by the {SOURCE_VERSION} → {TARGET_VERSION} upgrade path.
5. Identify state-file dependencies (remote backends, workspaces) that may
   require state surgery.
6. Note uses of `for_each`, `dynamic` blocks, or `lifecycle` rules that
   commonly break across major azurerm version boundaries.

### Output format

Produce this table (Markdown):

```
| Resource Type | File | Count | Breaking Change Risk |
|---------------|------|------:|----------------------|
| azurerm_xxx   | path | n     | none / low / high    |
```

End with a summary line: **files scanned | resource types found | high-risk
count**.

---

## Phase 2 — Breaking Changes Report

Cross-reference the inventory against the official azurerm CHANGELOG entries
between {SOURCE_VERSION} and {TARGET_VERSION}.

For every breaking change that applies to this codebase:

- State the change: removed resource / renamed argument / type change /
  behavior change.
- Name the affected HCL attribute or block.
- Show a minimal before → after code snippet (3–5 lines max, fenced `hcl`).
- Classify impact: **plan-time error** | **apply-time error** | **silent
  behavior change**.
- Flag if `terraform state mv` or `terraform import` is required.

### Grouping

1. **CRITICAL** — blocks `terraform plan` or `apply`.
2. **MODERATE** — deprecated; will break on next major.
3. **LOW** — cosmetic or optional.

Rules:

- Do NOT omit any breaking change found in the codebase.
- Mark state-manipulation steps with: **⚠ STATE SURGERY REQUIRED**.
- When a `terraform state mv` is needed, emit the exact command — never
  paraphrase it.

End with a summary line: **critical | moderate | low counts**.

---

## Phase 3 — Workspace Migration & Iterative Validation

The codebase uses Terraform workspaces for environment separation. Migration
proceeds sequentially: **atlas-dev → atlas-uat → atlas-production**.

The code changes are made once (they are shared across workspaces). What
differs per workspace is the validation and apply cycle, because each
workspace targets a different var-file and state.

If the user declares different workspace names, adapt accordingly.

### 3.1 — Pre-migration (run once, before any workspace)

1. Back up all workspace state files (remote snapshot or `terraform state pull`
   per workspace).
2. Verify no state locks are held: `terraform force-unlock` if stale.
3. Apply code changes from Phase 2 (provider pin to {TARGET_VERSION},
   resource/attribute fixes).
4. Run `terraform init -upgrade` to pull the new provider.

### 3.2 — Iterative validation loop (run per workspace)

For each workspace, select it and run the validation loop below. Do NOT
advance to the next workspace until the current one passes cleanly.

```shell
terraform workspace select <workspace>
```

**The loop:**

```
┌─► terraform fmt -recursive -check
│   ↓ (fix formatting issues if any)
│   terraform validate
│   ↓ (fix schema/syntax errors if any)
│   terraform plan -var-file=<environment>.tfvars
│   ↓
│   Analyze plan output:
│     • 0 destroy / recreate on critical resources? → proceed
│     • unexpected diff? → diagnose, fix code or state, restart loop
│     • ⚠ STATE SURGERY needed? → run exact state mv/import commands
│       from Phase 2, then restart loop
│   ↓
└── If clean: workspace validated ✅
    If not: reiterate ↑
```

Var-file mapping (defaults — adapt if the user states otherwise):

| Workspace          | Var-file              |
|--------------------|-----------------------|
| atlas-dev          | dev.tfvars            |
| atlas-uat          | uat.tfvars            |
| atlas-production   | production.tfvars     |

### 3.3 — Workspace-specific constraints

| Workspace          | Rules |
|--------------------|-------|
| atlas-dev          | Full migration, break-fix allowed, no approval gate. Reiterate the validation loop freely until plan is clean. |
| atlas-uat          | Must produce the same plan shape as atlas-dev (same change set, zero unexpected destroys). Require clean plan before apply. |
| atlas-production   | Require peer-review sign-off, maintenance window, and snapshot/backup of stateful resources (databases, storage accounts) before apply. Plan must show 0 destructive changes on critical resources. |

### 3.4 — Apply & rollback

There are no manual `terraform apply` executions. All applies run through a
CI/CD pipeline operating under an elevated Service Principal. The engineer
prepares the code, validates locally (3.2), performs any state surgery, then
hands off to the pipeline.

**Engineer responsibilities per workspace:**

1. Validation loop (3.2) passes cleanly.
2. Commit and push the migration branch.
3. For atlas-production: confirm peer-review sign-off, maintenance window,
   and stateful resource backups are in place before the pipeline is triggered.

**Pipeline execution:** the pipeline handles `plan → approve → apply`
under the elevated SP. The skill does not have permissions to trigger or
manage pipeline runs — the engineer triggers them through the normal CI/CD
workflow.

**If the pipeline apply fails:** review pipeline logs and plan output. Do not
re-trigger blindly. If resources are in a partial state, restore from the
pre-migration state backup. 

End with a summary per workspace: **resources affected | plan changes |
risk level**.

---

## Clarification Behavior

If you encounter any of the following, **STOP and ask one focused question
before proceeding** (never batch multiple ambiguities):

- Ambiguous resource ownership (multiple teams sharing one state file).
- Custom provider forks or wrapper modules shadowing azurerm resources.
- Missing variable definitions affecting resource naming or environment
  targeting.
- Workspace names or var-file mappings that don't match the defaults
  (atlas-dev / atlas-uat / atlas-production).
- Var-file contents that differ across environments in ways that affect
  which resources exist (conditional resource creation, count/for_each
  driven by variables).
- Resources in state but absent from code (orphaned state entries).
- Workspace state divergence — e.g. atlas-dev state has drifted from code
  while atlas-uat has not.
- CI/CD pipeline configuration unclear — e.g. no visible approval gate,
  missing pre-apply step for state surgery commands, or Service Principal
  RBAC scope unknown.

---

## Output Rules

- Show file paths relative to repo root.
- Use fenced code blocks: `hcl` for Terraform, `shell` for CLI commands.
- Never modify files without showing a diff first and receiving confirmation.
- Prefer idempotent operations in migration scripts.
- Emit exact `terraform state mv` / `terraform import` commands — no
  paraphrasing.
- At the end of each phase, emit a summary: **files changed | resources
  affected | risk level**.

---

## Changelog Research

When the codebase is available, look up breaking changes by:

1. Searching the web for `azurerm provider changelog {SOURCE_VERSION}
   {TARGET_VERSION}` and the official upgrade guide.
2. Fetching the HashiCorp / Azure provider upgrade documentation if a major
   version boundary is crossed (e.g. v3 → v4 has a dedicated guide).
3. Cross-referencing each resource type from the inventory against the
   retrieved changelog entries.

If no codebase is provided yet, ask the user to share it before starting
Phase 1.
