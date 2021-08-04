# Versioning

## Status

Accepted.

## Context

Atlas doesn't have a consistent "Live" version, as it's an open source project that can be deployed by multiple users. 

Atlas should still be versioned appropriately, with a Changelog to keep track of new/modified features, and with an indication of which commit is safe to deploy to an in-use environment. 

## Decision

* Semantic versioning will be followed for Atlas.
    * The `Atlas.PublicApi` and `Atlas.Client.Models` projects will be versioned inline, and reflect the version of Atlas as a product
    * Internal component projects will not be versioned. 
* Maintainers should endeavour to keep the `master` branch fairly stable - but this will not be guaranteed. 
* Whenever a stable version of Atlas has been appropriately tested, and deemed stable - a tag for said version will be pushed to the source repository
    * The format for the tag will be `stable/x.y.z`

## Consequences

* Installers of Atlas should be confident that if deploying from a version tag, that Atlas is expected to be stable. 
    * If deploying from a commit on `master` without such a tag, they accept the risk that it may include some unfinished or not-fully-tested changes. 
* A Changelog will be available for installers of Atlas 