﻿# Atlas.Debug.Client

## Description
This package is a collection of Atlas debug endpoints intended for use by automated end-to-end tests.

## Changelog
- The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
- The debug client API has its own version number, which is maintained independently of the main Atlas version number.
- It also has a `TargetAtlasVersion` (see .csproj file) which is the version of Atlas that the debug API targets.

### 1.0.0 (targets Atlas v1.7.0)
* Creation of new library, `Atlas.Debug.Client`.