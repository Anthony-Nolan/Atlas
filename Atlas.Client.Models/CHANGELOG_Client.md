# Changelog
All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versions

### 1.1.0

#### Changed
- Renamed `HlaNomenclatureVersion` to `MatchingAlgorithmHlaNomenclatureVersion` on both result set and notification models,
  now that Matching and Match Prediction are able to use two different HLA versions.

### 1.0.0

- First stable release of Atlas client.