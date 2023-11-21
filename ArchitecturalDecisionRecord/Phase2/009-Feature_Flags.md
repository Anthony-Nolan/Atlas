# Feature Flags

## Status

Accepted.

## Context

The specific need to add feature flag (FF) infrastructure was the complexity around the release of issue #1064 which could not be deployed in one go. See issue description for more details.

## Decision

* Azure App Configuration will be used to manage FFs within Atlas.
* The app config resource and each individual FF will be defined using Terraform.
  * The resource definition will sit at the top-level.
  * Each FF definition should be placed within the module that it controls, e.g., FF that affects matching would be place in the `/matching` module.
  * An FF that controls multiple components should be defined at the top-level.
  * The default value for FFs on first release should always be "off" and Terraform should be instructed to ignore changes to the flag value.
* FFs will be documented in the [Atlas changelog](/Atlas.Functions.PublicApi/CHANGELOG_Atlas.md/#feature-flags).
* Notes on usage: 
  * At present, FFs will be primarily used as a short-term measure to permit finer control over release.
  * Control and configuration of feature behaviour should continue to be done via app settings and release variables.
  * Ideally, FFs should be removed entirely from the codebase ASAP to avoid techdebt and complexity.
    * For FFs that manage the release of significant code changes (which is the most expected use case), this should be done in the next minor release version.
    * Retirement of FFs should also be documented in the changelog.

## Consequences
* FFs proved very useful in the release of issue #1064.
* They should prove even more useful now that Atlas is live in two systems (Anthony Nolan and WMDA).
* However, they should be used sparingly as they add to the tech debt burden on the system and increase complexity of configuration, especially when considered with the many [release variables](/README_Configuration.md) there are in play.