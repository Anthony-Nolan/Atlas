﻿# Atlas.Debug.Client

## Description
This package is a collection of Atlas debug endpoints intended for use by automated end-to-end tests.

## Backwards Compatibility
The debug client is versioned in lockstep with the Atlas API. However, the same debug client package version may still be compatible with older versions of Atlas.
The following table documents backwards compatibility.

| Debug Client Version | Compatible With | Notes                                  |
|----------------------|-----------------|----------------------------------------|
| 2.1.0                | 2.1.0           | Debug client library first introduced. |

## Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### 3.1.1
* Creation of new client `Atlas.Debug.Client.SearchTrackingFunctionsClient`

### 3.0.0
* Updated .NET version from 6.0 to 8.0

### 2.5.0
* Made updatedBeforeDate non nullable in DonorImportFunctionsClient.GetExternalDonorCodesByRegistry
* Added Score and ScoreBatch endpoints to the Public API client.

### 2.1.0
* Creation of new library, `Atlas.Debug.Client`.
* Clients added for debug endpoints on the Public API, Donor Import, Matching Algorithm, and Top-level Function apps.