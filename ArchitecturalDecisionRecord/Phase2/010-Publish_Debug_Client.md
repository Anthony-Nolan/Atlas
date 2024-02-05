# Publish Client and Models for Atlas Debug endpoints

## Status

Accepted.

## Context
Several debug endpoints are being added to various Atlas functions apps for use by the upcoming end-to-end (E2E) test project.
As they are spread across different apps, it makes sense to collate them into a single client to make it easier to arrange tests, execute workflows, and assert outcomes.

## Decision
* Two new projects will be added:
  * `Atlas.Debug.Client.Models` - will hold any models that are uniquely referenced by debug endpoints.
  * `Atlas.Debug.Client` - a collection of the debug endpoints.
* Models are published separately so that Atlas functions projects that host debug endpoints need only reference the models package, and not the entire client library.
* Libraries will be published as NuGet packages in the same manner as existing packages (see [ADR 008](008-Publish_NuGet_Packages.md)).
* These packages will be versioned independently of the main Atlas solution, and the `.csproj` file will note the Atlas version number that the library targets, via the XML tag, `<TargetAtlasVersion>`.
  * This means changes to the debug client will not require a bump to the main Atlas version.
* Git tag, `debug/<version>/atlas-<targetAtlasVersion>`, will be used to mark new stable versions of the debug libraries.
  * This tag communicates that a single debug client version maybe compatible with multiple versions of Atlas.

## Consequences
* Atlas Debug client and models are now built as independently versioned NuGet packages that can be published to either a private or public feed, e.g., Azure Artifacts or NuGet.org, making it easier to write E2E tests against Atlas.