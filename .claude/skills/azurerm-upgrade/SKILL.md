---
name: azurerm-upgrade
description: >
  Assists with migrating Atlas's Terraform codebase between any two versions of
  the azurerm provider (e.g. v3→v4, minor bumps, or patch upgrades). Use this
  skill whenever the user mentions upgrading, migrating, or bumping the azurerm
  Terraform provider — including phrases like "upgrade azurerm", "migrate to
  azurerm v4", "bump provider version", "terraform provider upgrade azure", or
  "breaking changes azurerm". Also trigger when the user asks about azurerm
  changelog analysis, state surgery after a provider bump, or environment
  rollout plans for a Terraform provider change. Covers major, minor, and patch
  version transitions.
allowed-tools:
  - Read
  - Edit
  - Write
  - Grep
  - Glob
  - Bash
  - WebSearch
  - WebFetch
  - AskUserQuestion
  - Skill
  - mcp__terraform__get_latest_provider_version
  - mcp__terraform__get_provider_details
  - mcp__terraform__get_provider_capabilities
  - mcp__azure__get_azure_bestpractices
---

# AzureRM Provider Upgrade Skill

Guide an experienced Terraform / Azure engineer through a structured, phased
migration of Atlas's azurerm provider from **{SOURCE_VERSION}** to
**{TARGET_VERSION}**.

> **First step every time:** ask the user for the source and target provider
> versions if they haven't stated them. Pin both values and use them
> consistently throughout all phases.

## Context assumptions

- The engineer is experienced with Terraform and Azure.
- This repo has **two independent Terraform roots**, each with their own
  `versions.tf` provider constraint and their own `.terraform.lock.hcl`:
  `terraform/core` and `terraform/system-tests`. Both must be upgraded —
  check each one independently in every phase, don't assume a single root.
- The codebase uses Terraform workspaces for environment separation. Atlas's
  actual workspaces and var-files are:

  | Workspace          | Var-file (relative to `terraform/core`) |
  |---------------------|------------------------------------------|
  | `atlas-dev`         | `../dev.tfvars`                           |
  | `atlas-uat`         | `../uat.tfvars`                           |
  | `atlas-wmda-uat`    | `../wmda-uat.tfvars`                      |
  | `atlas-live`        | `../live.tfvars`                          |
  | `atlas-wmda-live`   | `../wmda-live.tfvars`                     |

  Do not fall back to generic defaults like `atlas-production` /
  `production.tfvars` — they don't exist in this repo. If the user's request
  implies different names, treat that as one of the ambiguities in
  "Clarification Behavior" below and confirm before proceeding.
- Remote backend (same for every workspace, all environments):
  `storage_account_name=novaterraform`, `container_name=terraform-state`,
  `resource_group_name=AN-RESOURCE-GROUP`.
- All `terraform apply` runs execute through a CI/CD pipeline under an
  elevated Service Principal — no manual applies, in any environment.
- Operate in a consultancy context: be precise, structured, and concise.
- This repo also has a `/terraform-plan <env>` slash command that runs
  init → workspace select → fmt → validate → plan for one environment with
  all of the above already wired up correctly — reuse it in Phase 3 instead
  of re-deriving the loop by hand.

---

## Phase 0 — Resume check

Before doing anything else, check the `terraform/` directory for existing
`azurerm-*.md` artifacts from a prior run of this skill (e.g.
`azurerm-migration-plan.md`, `azurerm-upgrade-guide.md`, or a
version-specific changelog file). If any exist:

- Read them first.
- Confirm with the user whether to resume from that state (skip phases
  already completed) or start over.
- If resuming, keep using the same filenames/version pair rather than
  creating parallel ones.

## Phase 1 — Codebase Analysis

Before proposing any changes, scan the full Terraform codebase and produce a
structured inventory — **separately for `terraform/core` and
`terraform/system-tests`**, since they version and apply independently.

Steps:

1. List every `azurerm_*` resource type and data source in use, per root.
2. For each, record the file path (relative to repo root) and approximate
   usage count.
3. Identify the provider version constraint in each root's `versions.tf`
   (`required_providers`, `version = "~> X.Y"`, etc.) and its
   `.terraform.lock.hcl`.
4. Look up the {SOURCE_VERSION} → {TARGET_VERSION} upgrade path using, in
   order of preference:
   1. `mcp__terraform__get_latest_provider_version` /
      `get_provider_details` / `get_provider_capabilities` for the azurerm
      provider — structured, authoritative resource/version data.
   2. `WebSearch` / `WebFetch` against the official HashiCorp azurerm
      CHANGELOG and, for major-version boundaries, the dedicated upgrade
      guide (e.g. the v3→v4 guide).
5. Flag deprecated arguments, removed resources, or renamed attributes
   affected by the upgrade.
6. Identify state-file dependencies (remote backend, workspaces) that may
   require state surgery.
7. Note uses of `for_each`, `dynamic` blocks, or `lifecycle` rules that
   commonly break across major azurerm version boundaries.

### Output format

Produce this table (Markdown), one per Terraform root:

```
| Resource Type | File | Count | Breaking Change Risk |
|---------------|------|------:|----------------------|
| azurerm_xxx   | path | n     | none / low / high    |
```

End with a summary line: **files scanned | resource types found | high-risk
count**.

**Persist this phase's output** to `terraform/azurerm-migration-plan.md`
(create or update — see Phase 0), so it survives across sessions.

---

## Phase 2 — Breaking Changes Report

Cross-reference the Phase 1 inventory (both roots) against the azurerm
CHANGELOG entries between {SOURCE_VERSION} and {TARGET_VERSION}.

For every breaking change that applies to this codebase:

- State the change: removed resource / renamed argument / type change /
  behavior change.
- Name the affected HCL attribute or block, and which root(s) it appears in.
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
- Before writing any azurerm resource code, invoke
  `mcp__azure__get_azure_bestpractices` and factor its guidance into the
  proposed fix.

End with a summary line: **critical | moderate | low counts**.

**Persist this phase's output** to `terraform/azurerm-changelog.md` (or the
existing versioned changelog filename found in Phase 0).

---

## Phase 3 — Workspace Migration & Iterative Validation

Migration proceeds sequentially by risk tier: **atlas-dev → atlas-uat →
atlas-wmda-uat → atlas-live → atlas-wmda-live**. Both `atlas-live` and
`atlas-wmda-live` are production and carry the same constraints (3.3).

The code changes are made once per root (they are shared across workspaces
within that root). What differs per workspace is the validation and apply
cycle, because each workspace targets a different var-file and state.

If the user declares different workspace names, adapt accordingly (and flag
the mismatch per "Clarification Behavior").

### 3.1 — Pre-migration (run once per Terraform root, before any workspace)

1. Back up all workspace state files (remote snapshot or `terraform state pull`
   per workspace) for that root.
2. Verify no state locks are held: `terraform force-unlock` if stale.
3. Apply code changes from Phase 2 (provider pin to {TARGET_VERSION},
   resource/attribute fixes) to that root.
4. From the root directory, run:

   ```bash
   terraform init -upgrade \
     -backend-config="storage_account_name=novaterraform" \
     -backend-config="container_name=terraform-state" \
     -backend-config="resource_group_name=AN-RESOURCE-GROUP"
   ```

   to pull the new provider and update `.terraform.lock.hcl`. Repeat for
   `terraform/system-tests` — it has its own lock file and provider
   constraint.

### 3.2 — Iterative validation loop (run per workspace, `terraform/core` only —
`system-tests` is not workspace-per-environment; validate it once against its
own state after 3.1)

For each of the 5 workspaces in turn, run:

```
/terraform-plan <env>
```

(`<env>` = `dev`, `uat`, `wmda-uat`, `live`, `wmda-live`). This already
selects the right workspace, applies `terraform fmt -recursive`, runs
`terraform validate`, and plans against the correct var-file — do not
re-derive these steps by hand. Do NOT advance to the next environment until
the current one passes cleanly.

**Analyze each plan's output:**

- 0 destroy / recreate on critical resources? → proceed to the next
  environment.
- Unexpected diff? → diagnose, fix code or state, rerun `/terraform-plan
  <env>` for the same environment before moving on.
- ⚠ STATE SURGERY needed? → run the exact `state mv`/`import` commands from
  Phase 2, then rerun `/terraform-plan <env>`.

If clean: workspace validated ✅. If not: reiterate on the same workspace.

### 3.3 — Workspace-specific constraints

| Workspace                          | Rules |
|-------------------------------------|-------|
| `atlas-dev`                         | Full migration, break-fix allowed, no approval gate. Reiterate the validation loop freely until plan is clean. |
| `atlas-uat`, `atlas-wmda-uat`       | Must produce the same plan shape as `atlas-dev` (same change set, zero unexpected destroys). Require clean plan before apply. |
| `atlas-live`, `atlas-wmda-live`     | Require peer-review sign-off, maintenance window, and snapshot/backup of stateful resources (databases, storage accounts) before apply. Plan must show 0 destructive changes on critical resources. |

### 3.4 — Apply & rollback

There are no manual `terraform apply` executions. All applies run through a
CI/CD pipeline operating under an elevated Service Principal. The engineer
prepares the code, validates locally (3.2), performs any state surgery, then
hands off to the pipeline.

**Engineer responsibilities per workspace:**

1. Validation loop (3.2) passes cleanly.
2. Commit and push the migration branch, following this repo's conventions:
   branch name `atl-<ticket>-<short-description>`, commit messages prefixed
   `chore: ATL-<ticket>: ...` (see `README_Contribution_Versioning.md`).
3. For `atlas-live` / `atlas-wmda-live`: confirm peer-review sign-off,
   maintenance window, and stateful resource backups are in place before the
   pipeline is triggered.

**Pipeline execution:** the pipeline handles `plan → approve → apply`
under the elevated SP. The skill does not have permissions to trigger or
manage pipeline runs — the engineer triggers them through the normal CI/CD
workflow.

**If the pipeline apply fails:** review pipeline logs and plan output. Do not
re-trigger blindly. If resources are in a partial state, restore from the
pre-migration state backup.

**Once every workspace is clean**, invoke the `terraform-reviewer` skill for
a WAF-aligned review pass over the changed files before handoff, rather than
treating validation as the final review step.

End with a summary per workspace: **resources affected | plan changes |
risk level**.

**Persist this phase's output** to `terraform/azurerm-upgrade-guide.md`,
consolidating the migration plan and changelog into a rollout runbook with
current status per workspace — this is what a follow-up session should read
first (Phase 0).

---

## Clarification Behavior

If you encounter any of the following, **STOP and ask one focused question
before proceeding** (never batch multiple ambiguities):

- Ambiguous resource ownership (multiple teams sharing one state file).
- Custom provider forks or wrapper modules shadowing azurerm resources.
- Missing variable definitions affecting resource naming or environment
  targeting.
- Workspace names or var-file mappings that don't match Atlas's actual set
  (`atlas-dev` / `atlas-uat` / `atlas-wmda-uat` / `atlas-live` /
  `atlas-wmda-live`).
- Var-file contents that differ across environments in ways that affect
  which resources exist (conditional resource creation, count/for_each
  driven by variables).
- Resources in state but absent from code (orphaned state entries).
- Workspace state divergence — e.g. `atlas-dev` state has drifted from code
  while `atlas-uat` has not.
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
- Keep generated/edited HCL within this repo's `.editorconfig` line-length
  limit (150 chars).
- At the end of each phase, emit a summary: **files changed | resources
  affected | risk level**, and persist the phase's output to the matching
  `terraform/azurerm-*.md` file as noted above.

---

## Changelog Research

1. Prefer the Terraform MCP server first: `mcp__terraform__get_latest_provider_version`
   and `mcp__terraform__get_provider_details` / `get_provider_capabilities`
   for the azurerm provider give structured, current version and resource
   schema data without needing to scrape docs.
2. Fall back to `WebSearch` for `azurerm provider changelog {SOURCE_VERSION}
   {TARGET_VERSION}` and `WebFetch` for the official HashiCorp / Azure
   provider upgrade guide when a major version boundary is crossed (e.g.
   v3 → v4 has a dedicated guide) — MCP tools give version/schema facts but
   not narrative breaking-change writeups.
3. Cross-reference each resource type from the inventory against the
   retrieved changelog entries.

If no codebase is provided yet, ask the user to share it before starting
Phase 1.
