---
description: "Run Terraform init, workspace select, fmt, validate, and plan for the Atlas core infrastructure against a chosen environment"
argument-hint: "<dev|uat|live|wmda-uat|wmda-live> [extra -var/-target flags]"
---

# Terraform Core Plan

Initialise the Terraform backend, select the matching workspace, and run a plan against the given environment's variable file.

## Environment

The first argument (`$1`) selects the environment. It must be one of:

| Environment | Workspace         | Var file                    |
|-------------|--------------------|------------------------------|
| `dev`       | `atlas-dev`         | `terraform/dev.tfvars`       |
| `uat`       | `atlas-uat`         | `terraform/uat.tfvars`       |
| `live`      | `atlas-live`        | `terraform/live.tfvars`      |
| `wmda-uat`  | `atlas-wmda-uat`    | `terraform/wmda-uat.tfvars`  |
| `wmda-live` | `atlas-wmda-live`   | `terraform/wmda-live.tfvars` |

If `$1` is missing or not one of the values above, stop and ask the user which environment to plan for — do not guess or default to one.

Any remaining arguments after `$1` (e.g. `-target=module.foo`) are extra flags to append to the `terraform plan` command.

## Guardrails

- DON'T ever run `terraform apply` as part of this workflow.
- This command is strictly for read-only checks (`terraform validate` and `terraform plan`).
- `live` and `wmda-live` are production environments — proceed exactly as for any other environment (this remains a read-only plan), but double check the workspace/var-file pairing before running the plan.

## Procedure

All commands must be run from the `terraform/core` directory.

```bash
cd terraform/core
```

### 1. Initialise with remote backend

```bash
terraform init -reconfigure \
  -backend-config="storage_account_name=novaterraform" \
  -backend-config="container_name=terraform-state" \
  -backend-config="resource_group_name=AN-RESOURCE-GROUP"
```

### 2. Select the workspace for the chosen environment

```bash
terraform workspace select atlas-<env>
```

Using the workspace name from the table above for the given `$1`.

### 3. Format all Terraform files recursively

```bash
terraform fmt -recursive
```

### 4. Validate the configuration

```bash
terraform validate
```

Fix any errors reported by `validate` before proceeding.

### 5. Run the plan

```bash
terraform plan -var-file ../<env>.tfvars
```

Using the var-file from the table above for the given `$1`, and appending any extra flags supplied after `$1`.
