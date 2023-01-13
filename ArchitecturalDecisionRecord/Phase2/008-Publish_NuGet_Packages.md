# Publish Models as NuGet Packages

## Status

Accepted.

## Context

Atlas is expected to be integrated into a larger search system in order to serve as a donor search algorithm service.
Publishing Atlas.Client.Models and its project dependencies as NuGet packages would make it significantly easier for consumers to interface with Atlas.

## Decision

* Two versions of Atlas.Client.Models will be packaged via the build.yml pipeline:
  * A pre-release version will be packaged, named after the Atlas version number and unique build-id, for release via continuous integration during development.
  * The second version will only be named after the Atlas version number is designed to be released once the build has been verified as stable.
* The Client.Models project is currently dependent on a few classes within the Atlas.Common project.
  * The Common project is quite large as it contains code that is shared across multiple components.
  * Those few models referenced by both Client.Models and other components will be moved to a new class library, Atlas.Common.Public.Models, which will also be published as a NuGet package.
  * The new library will be have the same version number as the Public API and Client.Models, but will have its own Changelog.

## Consequences

* Atlas client models are now built as NuGet packages that can be published to either a private or public feed, e.g., Azure Artifacts or NuGet.org, making it easier for consumers to integrate with Atlas.
* A less desirable consequence is having to add another class library to the solution, which already has several.
  * Further, the `dotnet pack` DevOps task does not automatically pack project dependencies, which means having to publish multiple packages, one for each dependency.