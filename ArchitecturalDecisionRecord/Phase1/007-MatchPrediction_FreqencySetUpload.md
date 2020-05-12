# Match Prediction - Frequency Set Upload

## Status

Proposed.

## Context

Admins of the Atlas installation should be able to upload a new frequency dataset, optionally for a specific registry or 
registry/ethnicity. This new dataset should replace the existing equivalent, which should be maintained but marked as obsolete.

## Decision

Import will be triggered by Azure Blob Storage Trigger, drawing registry and ethnicity details from filename.
Http endpoint will be made available as a public interface to enable ease of upload while specifying e.g. registry and ethnicity 
details via JSON rather than filename convention.

## Consequences

Upload to blob storage via HTTP rather than just parsing the file as part of the HTTP upload:

- Will lead to a slightly more unintuitive architecture to support
- Enables an implicit audit of the raw, unprocessed data as provided, which could be useful for debugging/support purposes
- Enables re-import of existing data in the event of e.g. a bugfix or schema change, without the need for the consumer to re-upload
all frequency sets to the HTTP endpoint