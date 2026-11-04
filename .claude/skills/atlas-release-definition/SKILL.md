---
name: atlas-release-definition
description: >
  Read or change the "Atlas Release" classic release definition in Azure DevOps (ADO): stage-scoped or release-scoped
  variables, and other edits such as tasks, approvals, triggers or stage settings. Every change gets a comment in
  the definition's history. Use this skill whenever the user wants to add, set, change, remove or show a release
  variable or a stage variable, a TF_VAR_ value, or a per-environment Terraform override in the release pipeline
  ("set TF_VAR_X to false in UAT and LIVE", "what is TF_VAR_Y in WMDA LIVE?"), or to change anything else in
  the Atlas release definition or ADO release pipeline, even if they don't name the definition.
---

# Atlas Release definition

Per-environment Terraform values for Atlas are stage-scoped variables named `TF_VAR_<NAME>` in the ADO classic release
"Atlas Release". A variable left at its Terraform default has no release variable at all. This skill reads and
changes that definition through `scripts/release_def.sh`. The script does the risky parts:

- it shows every change as a dry run first;
- it saves exactly the reviewed version, with the history comment;
- the save fails if someone else saved in the meantime;
- it reads the definition back after the save to check it.

The target is fixed: org `https://dev.azure.com/anthony-nolan-nova`, project `Atlas`, definition id 1
("Atlas Release").

## Authentication

The user runs `az devops login --organization https://dev.azure.com/anthony-nolan-nova` with a PAT in their own
terminal. The script uses `az devops invoke`, which picks up that login. Never ask for the PAT, never set
`AZURE_DEVOPS_EXT_PAT`, and never run `az devops login` yourself. If the script reports an authentication error,
ask the user to run the login command and tell you when it's done.

## Commands

Run from the repo root. `S=.claude/skills/atlas-release-definition/scripts/release_def.sh`

| Command | Writes? | What it does |
|---|---|---|
| `$S stages` | no | Stage ids, ranks, names, and the current revision |
| `$S show '<jq expression>'` | no | Any part of the definition, with secret values masked |
| `$S show-var <NAME>` | no | Value at release level, in each stage, and in each linked variable group (secrets show as `<secret>`) |
| `$S history [N]` | no | Last N revisions: number, date, author, comment |
| `$S set-var <NAME> <VALUE> (--stage "<name>")... \| --release` | no (dry run) | Add a variable or change its value. Other properties of the variable are kept |
| `$S unset-var <NAME> (--stage "<name>")... \| --release` | no (dry run) | Remove a variable |
| `$S edit <filter.jq>` | no (dry run) | Apply a jq program you write to the whole definition, for anything else |
| `$S apply <workdir> --comment "<text>"` | **yes** | Save the candidate from a dry run, then check it |

Stage names must match exactly, for example `--stage "WMDA LIVE"`. A dry run prints the base revision, a work dir and
a `diff -u` of the change. Exit code 3 means there is nothing to change.

Before any write, the script refuses a candidate that:
- adds, changes or removes a **secret** variable. Secrets are changed in the ADO UI by the user, so a secret value
  never passes through Claude;
- changes the definition's id, name or path, or adds, removes or renumbers a stage;
- has no change, or comes with an empty comment.

## Procedure

1. **Understand the change.** Work out what to change, in which stages (or at release level), the value, and why.
   Get the Jira ticket from the request, the conversation or the branch name (`atl-123-…`). If there is none, ask
   for it, because the history comment needs it.
2. **Look first.** Run `$S stages` and `$S show-var <NAME>`. A value in a linked variable group or at release level
   also applies to the stages, so mention it if it conflicts with the change. For other edits, read the relevant
   part of the definition with `$S show`, for example
   `$S show '.environments[] | select(.id == 27) | [.deployPhases[].workflowTasks[] | {name, enabled}]'`.
3. **For `TF_VAR_<NAME>`, check the Terraform side** in `terraform/core/variables.tf`:
   - The variable should be declared. If it isn't (for example, it is only on an unmerged branch), say so. Terraform
     only warns about an undeclared `TF_VAR_`, so it is harmless, but it does nothing until the declaration is
     released.
   - Read its type and any `validation`. If a validation refers to another variable (for example, precomputation off
     needs `MATCH_PREDICTION_PRECOMPUTE_MODE = ForceLive`), run `show-var` on that variable in the same stages. A
     wrong value would make the release fail.
   - Release variables are strings: use `false`, `500` and `ForceLive`, without HCL quoting. For lists or maps, use
     the HCL or JSON text Terraform expects, in the style of the existing values (`show-var` shows them).
4. **Draft the history comment** in the form `<TICKET>: <what changed: the variable, the stages, the value>. <why>.`
   For example: `ATL-488: add TF_VAR_MATCHING_DONOR_GENOTYPE_PRECOMPUTATION_ENABLED = false to UAT and LIVE. The
   Anthony Nolan environments do not use Match Prediction, so ...`
5. **Dry run.** Run `set-var`, `unset-var` or `edit`, and show the user the diff and the comment.
6. **Confirm when needed.**
   - **Apply straight after the dry run** when the user stated the variable, the stages and the value, and the diff
     shows exactly that.
   - **Ask for an explicit go-ahead first** for anything else: any `edit` (tasks, approvals, triggers, conditions,
     stage settings), any `unset-var`, a change you inferred rather than were told, or a diff with more in it than
     expected.
7. **Apply.** Run `$S apply <workdir> --comment "<comment>"`, and report what it prints:
   - the new revision;
   - "the saved definition matches the reviewed candidate";
   - "secret variables unchanged".

   If the save fails because the definition changed in the meantime, run the dry run again, and show the new diff
   if it differs. Never force or retry the save blindly. If the check after the save reports a difference, show it
   to the user. The work dir is kept so it can be inspected.
8. **Mirror `TF_VAR_` changes in the local tfvars**, so local plans match. These files are gitignored local copies:

   | Stage | File |
   |---|---|
   | DEV | `terraform/dev.tfvars` |
   | UAT | `terraform/uat.tfvars` |
   | LIVE | `terraform/live.tfvars` |
   | WMDA UAT | `terraform/wmda-uat.tfvars` |
   | WMDA LIVE | `terraform/wmda-live.tfvars` |

   Write `<NAME> = <value>` without the `TF_VAR_` prefix. Quote strings, and don't quote bools or numbers. Validation
   and the Publish Packages stages have no file.
9. **Report** the revision, each stage and value, and that the change takes effect on the next release of each
   stage. Saving the definition deploys nothing.

## Writing `edit` filters

Keep a filter as narrow as possible, and pick stages by `id` (from `stages`), not by array position. Write it to a
file in the session's scratchpad, and delete it after the apply.

```jq
# Example: change one task input in the WMDA UAT stage (id 27). Check the input names first:
#   $S show '.environments[] | select(.id == 27) | .deployPhases[].workflowTasks[] | select(.name == "Plan Terraform (Core)") | .inputs'
.environments |= map(
  if .id == 27 then
    .deployPhases[].workflowTasks |= map(if .name == "Plan Terraform (Core)" then .inputs.TerraformVersion = "1.14.8" else . end)
  else . end)
```

The parts of the definition:
- `variables`: release-level variables, `{ "<NAME>": { "value": "...", "isSecret": bool?, "allowOverride": bool? } }`
- `variableGroups`: linked group ids. Group 5 is "Terraform". Groups are changed through a different API, which this
  skill does not cover.
- `environments[]`: the stages, each with `id`, `name`, `rank`, `variables`, `variableGroups`, `conditions` (stage
  triggers), `preDeployApprovals`, `postDeployApprovals`, `deployPhases[].workflowTasks[]` (`name`, `taskId`,
  `enabled`, `inputs`) and `deployPhases[].deploymentInput`.
- `triggers`, `artifacts`: release triggers and build artifacts.

Stage ids when this skill was written: DEV 5, Publish Packages - Pre-release 13, UAT 11, LIVE 12, Publish Packages -
Stable 14, WMDA UAT 27, WMDA LIVE 28, Validation 16. Always confirm with `$S stages`.
