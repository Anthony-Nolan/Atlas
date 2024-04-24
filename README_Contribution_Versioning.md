# Contribution and Versioning

## Versioning

See [Version ADR](ArchitecturalDecisionRecord/Phase2/005-Versioning.md) for an explanation of Atlas versioning.

---

Atlas follows semantic versioning:

`major.minor.patch`

* major = large new feature sets, breaking changes to existing interfaces or features
* minor = small features, minor tweaks to application logic
* patch = bugfixes, small documentation improvements

Only the *public interface* of ATLAS is versioned in code. In practice, this means these projects within this solution:

* `Atlas.Functions.PublicApi`
  * The public HTTP interface that consumers should use for all HTTP requests to ATLAS
* `Atlas.*.Client.Models`
  * All client models expected to be needed by external consumers are within these projects.
* `Atlas.DonorImport.FileSchema.Models`
  * Models that represent the donor import file schema.
* `Atlas.Common.Public.Models`
  * All models referenced by API projects and other Atlas components.
* `Atlas.Debug.Client`
  * Client that collects all debug endpoints required to write automated end-to-end tests against Atlas.
* `Atlas.Debug.Client.Models`
  * Models used by debug endpoints and/or debug client methods.

All the above projects are versioned in-step with the public API.

## Releases

Atlas has no central host, so it is expected that any registry running a copy of the Atlas application will manage their own release pipeline.

From a centralised perspective, Atlas is not released as an application, nor published to a package feed as a code library.

Instead, stable versions of Atlas will be tagged in the following format:

`stable/x.y.z`, where `x.y.z` is the [version](#versioning) of Atlas.

Maintainers of Atlas instances should aim to only deploy from the `stable` tag - other commits may exist on the `master` git branch, but will not have been fully tested or signed off.

In order to "release" code in this manner, someone at Anthony Nolan should sign off on a batch of changes to be released.

This sign-off process should involve ensuring:

* All automated tests have passed.
* Manual testing of the algorithm has been performed, if deemed necessary.
* If algorithmic logic has changed, an expert in the field of HLA matching must sign off the algorithmic changes.
* Documentation has been updated as appropriate, notably:
    * [Feature CHANGELOG](./Atlas.Functions.PublicApi/CHANGELOG_Atlas.md)
    * [Client CHANGELOG](./Atlas.Client.Models/CHANGELOG_Client.md)
    * [Debug Client CHANGELOG](./Atlas.Debug.Client/CHANGELOG_DebugClient.md) 
    * [Debug Client Model CHANGELOG](./Atlas.Debug.Client.Models/CHANGELOG_DebugClientModels.md)   
    * Database changelogs (see invidivudal `.Data` projects)

### Other Tags
* The `unverified/x.y.z` tag will be used occasionally to manage the release of versions to non-production environments.

## Contributing

To contribute to Atlas, the following steps should be taken: 

1. Ensure there is [an appropriate Github Issue](https://github.com/Anthony-Nolan/Atlas/issues) for the change being made - if one does not exist, please create one with a description of why the change is necessary, using the templates provided.
2. Fork the repository, and make any code changes on a branch of your fork.
3. Ensure commit messages are prefixed with the GitHub issue # and follow [semantic commit messaging](https://seesparkbox.com/foundry/semantic_commit_messages).
  - Examples:
  - `chore: #12: add new component to build script`
  - `docs: #23: explain HLA nomenclature`
  - `feature: #34: add match prediction`
  - `fix: #45: fix null reference`
  - `refactor: #56: extract helper class for locus selection`
  - `style: #67: convert tabs to spaces`
  - `test: #78: add validation tests for new feature`
  - `review: #89: review markups`
3. Create a Github Pull Request from your fork branch to the master branch of this repository.
4. All code changes will then be reviewed by a code owner before they can be merged into the master branch.
5. If any testing fails post-merge, you may be asked by a repository owner to assist in fixes, as required.  