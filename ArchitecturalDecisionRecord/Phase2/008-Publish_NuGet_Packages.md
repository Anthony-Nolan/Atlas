# Publish Models as NuGet Packages

## Status

Accepted.

## Context

Atlas is expected to be integrated into a larger search system in order to serve as a donor search algorithm service.
Publishing API Models and their project dependencies as NuGet packages would make it significantly easier for consumers to interface with Atlas.

## Decision

* The following models will be published as NuGet packages:
  - Donor import file schema
  - Matching Algorithm client
  - Public API client (i.e., Search and Repeat Search)
* Two versions of each project will be packaged via the build.yml pipeline:
  * A pre-release version, named after the Atlas version number and unique build-id, for release via continuous integration during development.
  * The second version, named after the Atlas version number only, is designed to be released once the build has been verified as stable.
* The API projects are currently dependent on a few classes within the Atlas.Common project.
  * The Common project is quite large as it contains code that is shared across multiple components.
  * Those few models referenced by both API models and other components will be moved to a new class library, Atlas.Common.Public.Models, which will also be published as a NuGet package.
  * The new library will be have the same version number as the Public API, but will have its own Changelog.

## Consequences

* Atlas API models are now built as NuGet packages that can be published to either a private or public feed, e.g., Azure Artifacts or NuGet.org, making it easier for consumers to integrate with Atlas.
* A less desirable consequence is having to add more class libraries to the Atlas solution, which already has several.
  * Further, the `dotnet pack` DevOps task does not automatically pack project dependencies, which means having to publish multiple packages, one for each dependency.