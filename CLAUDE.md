# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Atlas is Anthony Nolan's open-source HLA (histocompatibility) donor-matching algorithm service — a "Donor Search Algorithm as a Service". It implements clinical guidelines for matching stem cell donors to patients based on HLA typing data, and is used in WMDA's Search & Match Service. It is a .NET 10 solution of ~30 projects deployed as a set of Azure Functions apps, backed by SQL Server (via EF Core) and Azure Storage/Service Bus.

The repo has extensive existing documentation — prefer it over re-deriving things from code:
- `README.md` — index of all other READMEs (component READMEs, deployment, configuration, integration, support, maintenance)
- `README_ArchitecturalOverview.md` — component breakdown and infrastructure per component
- `README_DevelopmentStartUpGuide.md` — full local dev setup ("Zero to Hero"): EF migrations, seeding donors/HLA data/haplotype frequency sets, running a search end-to-end
- `README_DevelopmentSettings.md` — settings/secrets patterns (Options pattern, `local.settings.json` scaffolding)
- `README_Contribution_Versioning.md` — versioning rules and commit message conventions
- `README_MatchingAlgorithm.md`, `README_MatchPredictionAlgorithm.md`, `README_HlaMetadataDictionary.md`, `README_DonorImport.md`, `README_MultipleAlleleCodeDictionary.md`, `README_RepeatSearch.md` — per-component deep dives
- `ArchitecturalDecisionRecord/` — ADRs explaining why past architectural decisions were made
- `.github/skills/terraform-core-dev-plan/`, `.github/skills/azurerm-upgrade/` — Claude skills for this repo's Terraform workflows (dev plan, provider upgrades)

## Build and test commands

```bash
# Restore + build (mirrors build-pipeline.yml)
dotnet restore
dotnet build --configuration Release --no-restore

# Run all unit tests (Test projects only, excludes Integration/Validation/Performance/Verification)
shopt -s globstar
dotnet test **/*Test.csproj

# Run a single test project
dotnet test Atlas.MatchingAlgorithm.Test/Atlas.MatchingAlgorithm.Test.csproj

# Run a single test by name
dotnet test Atlas.MatchingAlgorithm.Test/Atlas.MatchingAlgorithm.Test.csproj --filter "FullyQualifiedName~RankSearchResults_OrdersResultsByMatchCount"

# EF Core migrations — must be run from inside the relevant .Data project folder
# Apply pending migrations:
dotnet ef database update -p <ProjectName>
# Create a new migration after changing a data model:
dotnet ef migrations add -p <ProjectName> <MigrationName>
```

Data projects with their own EF Core migrations: `Atlas.MatchingAlgorithm.Data`, `Atlas.MatchingAlgorithm.Data.Persistent`, `Atlas.DonorImport.Data`, `Atlas.MatchPrediction.Data`, `Atlas.RepeatSearch.Data`, `Atlas.SearchTracking.Data`. The Matching Algorithm uniquely maintains **two** transient databases ("A"/"B", for hot-swapping during data refresh) — migrations must be run against both when working on that schema.

Test projects follow a strict per-component split, each with different infra requirements:
- `*.Test` — pure unit tests, no external dependencies.
- `*.Test.Integration` — hits a real SQL database; Azure Storage Emulator (Azurite) must be running; other dependencies (HMD, etc.) are stubbed.
- `*.Test.Validation` (Matching, Match Prediction) — spins up a real in-memory server and exercises it over HTTP; written in Gherkin via Reqnroll for non-technical review by the Search/BioInformatics teams; requires user secrets and a specific HLA Metadata Dictionary version to be generated locally first.
- `*.Test.Performance` / `*.Test.Verification` — manual/CI-nightly performance harnesses, gated by the custom `IgnoreExceptOnCiPerfTestAttribute` (only runs when `RUN_CI_PERF_TESTS` env var is set).

There is no local Azure Service Bus emulator in use for this project — local dev requires a real Azure Service Bus namespace with manually-created topics/subscriptions (see `README_DevelopmentStartUpGuide.md` for the exact topic/subscription names needed per workflow). Microsoft does ship a dockerized Service Bus emulator, but it isn't adopted here (it caps out at ~10 concurrent requests and is clunky to configure).

## Architecture

### Components (each with its own infra, README, and `Atlas.<Name>*` project family)

- **Donor Import** — master donor store; ingests donor JSON files from blob storage, feeds Matching and Match Prediction.
- **HLA Metadata Dictionary (HMD)** — Azure Table Storage cache of HLA nomenclature (sourced from WMDA/IMGT-HLA files), used to interpret and convert between HLA typing representations (allele, G group, P group, "small g" group, serology, MAC). Nearly everything else depends on it for HLA interpretation.
- **Matching Algorithm** — the core, non-predictive search: given patient HLA, returns donors that meet mismatch criteria, then grades/scores/ranks matches. Matching is done at P-group level; see `README_MatchingAlgorithm.md` for grading/confidence/ranking rules and null-allele handling — this logic is clinically sensitive and changes need sign-off from an HLA matching expert (see `README_Contribution_Versioning.md`).
- **Match Prediction Algorithm** — post-processes Matching's results per patient/donor pair, computing likelihood of a given mismatch count using haplotype frequency sets.
- **Multiple Allele Code (MAC) Dictionary** — Azure Table Storage cache of NMDP allele-compression codes.
- **Repeat Search** — standalone component that tracks previously-returned donors so consumers can request differential results; kept separate to keep the main algorithm stateless.
- **Search Tracking** — tracks search lifecycle/state.
- **Atlas.Functions** — top-level Functions app; runs MAC import and orchestrates match prediction after a matching search completes.
- **Atlas.Functions.PublicApi** — the versioned public HTTP API surface. This, together with `Atlas.*.Client.Models`, `Atlas.DonorImport.FileSchema.Models`, `Atlas.Common.Public.Models`, and `Atlas.Debug.Client(.Models)`, is the only code versioned/released as public interface — see `README_Contribution_Versioning.md`.

Per component, the project-naming convention is: `Atlas.<Component>` (business logic), `Atlas.<Component>.Data` (EF Core schema + Dapper/EF querying), `Atlas.<Component>.Functions` (Azure Functions entry point), `Atlas.<Component>.Client.Models` (models for external consumers), `Atlas.<Component>.Common` (shared internal models between logic/data layers), plus the `.Test`/`.Test.Integration`/etc. suite.

### Cross-cutting code (`Atlas.Common`)

Shared code lives under `Atlas.Common/` by concern: `ApplicationInsights` (logging/telemetry), `AzureEventGrid`, `AzureStorage` (blob/table clients), `Caching`, `Debugging`, `FeatureManagement`, `GeneticData` (HLA typing model/utilities), `Matching` (locus match calculators), `Maths`, `Notifications`, `ServiceBus`, `Sql` (bulk insert helpers), `Utils`, `Validation`.

### Dependency injection

No MediatR/CQRS — this is plain `IServiceCollection` composition. Each subsystem exposes registration via extension methods (often named `Register*`, living in a `DependencyInjection/` folder or a `*Registration.cs`/`ServiceConfiguration.cs` file), which top-level entry points (Functions app `Startup.cs`, or the API) compose together. Component projects should only depend on plain `TSettings`, not `IOptions<TSettings>` — the entry-point app is responsible for resolving `IOptions<TSettings>` and re-registering it as `TSettings` for component-project consumption (see `README_DevelopmentSettings.md#options-pattern`). Don't violate this layering by injecting `IOptions<T>` directly into a component project.

### Messaging

Azure Service Bus access goes through a bespoke abstraction in `Atlas.Common/ServiceBus/` (`ITopicClient`/`TopicClientFactory`, `MessageReceiver`/`MessageReceiverFactory`, batch publish/receive helpers) rather than raw SDK calls — use these when adding new messaging. Keyed DI (`RegisterServiceBusAsKeyedServices`) is used where a component needs more than one Service Bus connection (e.g. matching bus vs. search-tracking bus).

### Settings

Non-Functions projects use `appsettings.json` + user secrets. Functions apps use `local.settings.json`, which is scaffolded from a checked-in `local.settings.template.json` on build (never edit `local.settings.json` in a way you expect to be preserved by git — it's gitignored; add new settings to the `.template.json` instead).

## Conventions

- Test framework: NUnit (`[TestFixture]`/`[Test]`), NSubstitute for mocking, AwesomeAssertions (FluentAssertions-compatible, `.Should()`) for assertions, AutoFixture (via a repo `FixtureBuilder.For<T>()` helper) plus custom `*Builder` classes under `TestHelpers/Builders` for test data. Test naming: `MethodUnderTest_ExpectedBehaviour`.
- Commit messages are prefixed with the Jira ticket ID and a semantic type, e.g. `feature: ATL-34: add match prediction`, `fix: ATL-45: fix null reference` (see `README_Contribution_Versioning.md` for the full type list — note that README documents an older GitHub-issue-number convention, e.g. `fix: #45: ...`, which real commit history confirms has been superseded by the `ATL-###` Jira convention).
- Versioning is semantic (`major.minor.patch`) and applies only to the public-interface projects listed above; stable releases are tagged `stable/x.y.z`.
- `.editorconfig` sets `max_line_length = 150`.

## Branching

Based on Anthony Nolan's org-wide [Git Standards](https://anthonynolan.atlassian.net/wiki/spaces/NPDWS/pages/532185178/Git+Standards) (Confluence, NPDWS space), adapted for Atlas: **`master` is the primary branch** most changes are merged into (Atlas has no separate `develop` branch).

- **Release candidate branches**: prefix `rc/`, named for the target minor version (e.g. `rc/3.4.0`), cut from `master` when a version is ready to test/release. Once cut, no new features land on it — only bug fixes. One `rc/` branch is active per in-progress minor version; delete it once no longer used in any environment.
- **Hotfix branches**: prefix `hotfix/` (e.g. `hotfix/3.1.1`), cut from the corresponding already-released `rc/` branch, for patching a version already live.
- **Feature/fix branches**: write changes on a feature branch first, then PR it in for review.
  - **Target branch rule**: if there's no `rc/`/`hotfix/` branch currently open for the release you're targeting, PR into `master`; if one is already open, PR into that `rc/`/`hotfix/` branch instead.
  - **Naming**: lowercase, hyphenated, starting with the Jira story card ID (use the story number even if a subtask is involved) followed by a short description — e.g. `atl-1234-add-search-patient-endpoint`. Avoid mixed case, a missing description, or a missing ticket ID.
- Any change merged into an `rc/`/`hotfix/` branch must also be merged back down through every subsequent `rc/`/`hotfix/` branch and into `master`, so it's present in all future versions too. Use a real merge (not rebase) for this propagation, to keep a readable record of which release each fix came from — feature branches merging *into* `master`/`rc/`/`hotfix/`, by contrast, should be rebased or squashed to keep history linear (via GitHub's "Rebase and merge"/"Squash and merge"; this repo doesn't use reviewable.io).

When creating a branch or opening a PR, check whether an `rc/`/`hotfix/` branch is currently open for the release you're targeting — if so, PR into that branch; otherwise `master` is correct.
