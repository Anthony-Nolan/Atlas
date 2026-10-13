# Publish Client and Models for Atlas Debug endpoints

## Status

Superseded - see [Update 2](#update-2).

## Context
Several debug endpoints are being added to various Atlas functions apps for use by the upcoming end-to-end (E2E) test project.
As they are spread across different apps, it makes sense to collate them into a single client to make it easier to arrange tests, execute workflows, and assert outcomes.

## Decision
* Two new projects will be added:
  * `Atlas.Debug.Client.Models` - will hold any models that are uniquely referenced by debug endpoints.
  * `Atlas.Debug.Client` - a collection of the debug endpoints.
* Models are published separately so that Atlas functions projects that host debug endpoints need only reference the models package, and not the entire client library.
* Libraries will be published as NuGet packages in the same manner as existing packages (see [ADR 008](008-Publish_NuGet_Packages.md)).
* [See [Update below](#update)] 
* ~~These packages will be versioned independently of the main Atlas solution, and the `.csproj` file will note the Atlas version number that the library targets, via the XML tag, `<TargetAtlasVersion>`.~~
  * ~~This means changes to the debug client will not require a bump to the main Atlas version.~~
* ~~Git tag, `debug/<version>/atlas-<targetAtlasVersion>`, will be used to mark new stable versions of the debug libraries.~~
  * ~~This tag communicates that a single debug client version maybe compatible with multiple versions of Atlas.~~

## Consequences
* Atlas Debug client and models are now built as independently versioned NuGet packages that can be published to either a private or public feed, e.g., Azure Artifacts or NuGet.org, making it easier to write E2E tests against Atlas.

## Update
* Versioning the debug client packages independently of the other Atlas packages did not prove feasible (see [this ticket](https://github.com/Anthony-Nolan/Atlas/issues/1295)). They will instead be versioned in step with Atlas, and instead the changelog should indicate the Atlas version range that the client is compatible with.

## Update 2
* The debug client proved to be an unnecessary HTTP layer over SQL, Service Bus and Blob Storage. The E2E test project, [Atlas.Auto.Tests](https://github.com/Anthony-Nolan/Atlas.Auto.Tests), now accesses these resources directly (see [ATL-356](https://github.com/Anthony-Nolan/Atlas.Auto.Tests/pull/73)).
* `Atlas.Debug.Client` and `Atlas.Debug.Client.Models` have been removed, and are no longer published from Atlas `4.1.0` onwards. `4.0.0` is the last version that has them.
* The debug endpoints that only wrapped a resource have been removed with them. The rule from now on:
  * No debug endpoint may only wrap a resource that can be accessed directly: SQL, Service Bus, Blob Storage or Application Insights.
  * A debug endpoint that needs Atlas code is allowed, e.g., the HLA Metadata Dictionary, the matching or match prediction algorithms, or key generation. It keeps the `/debug/` route prefix, lives in the functions app that owns that code, and keeps its request and response models in that app: they are not published.
* The HLA Metadata Dictionary endpoints (`ConvertHla`, `ScoringMetadata`, `Dpb1TceGroups`, `SerologyToAlleleMapping`) and the match prediction endpoints (`Impute`, `MatchPatientDonorGenotypes`) are kept under this rule.
