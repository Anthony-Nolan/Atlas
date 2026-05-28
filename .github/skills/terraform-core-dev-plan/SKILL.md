---
name: terraform-core-dev-plan
description: "Run Terraform init, workspace select, fmt, validate, and plan for the Atlas core infrastructure dev environment. Use when: running terraform plan for dev, initialising terraform core backend, switching to atlas-dev workspace, formatting core terraform files, validating core terraform configuration, planning atlas-dev core infrastructure changes."
argument-hint: "Optional: extra -var or -target flags to append to the plan command"
---

# Terraform Dev Plan

Initialise the Terraform backend, select the `atlas-dev` workspace, and run a plan against the dev variable file.

## When to Use

- You need to preview infrastructure changes for the `atlas-dev` environment
- You have just cloned the repo or need to reinitialise the backend
- You want to confirm the current workspace before running a plan

## Guardrails

- DONT ever run `terraform apply` as part of this validation/plan workflow.
- This skill is strictly for read-only checks (`terraform validate` and `terraform plan`).

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

### 2. Select the dev workspace

```bash
terraform workspace select atlas-dev
```

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
terraform plan -var-file ../dev.tfvars
```

If the user supplied extra flags (e.g. `-target=module.foo`), append them to the plan command above.
