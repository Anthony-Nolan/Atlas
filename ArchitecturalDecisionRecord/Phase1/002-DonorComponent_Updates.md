# Donor Component - Updates

## Status

Proposed.

## Context

The donor component of Atlas needs the ability to add/update/delete donors.
A JSON schema for differential donor updates has been provided by WMDA.
It was agreed with WMDA that uploading donor update files to a shared storage location was the preferred way of determining these updates.

## Decision

Azure Blob Storage will be used as the shared storage, into which donor update files can be uploaded.

## Consequences

- Consistency with other components of ATLAS - blob storage account already exists
- Allows the use of blob storage triggered functions to trigger updates as soon as files are uploaded, rather than needing to poll the 
file storage at regular intervals.