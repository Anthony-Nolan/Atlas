# Atlas.MatchingAlgorithm.Client.Models

## Description
This package contains all client models utilised by the Atlas Matching Algorithm API for data refresh, scoring-only, and matching-only requests.

* `DataRefresh` - request, response and completion message
* `Donors` - currently only contains `DonorType` enum, which is due to be moved out of the Client package (//TODO - [issue #763](https://github.com/Anthony-Nolan/Atlas/issues/763))
* `Scoring` - request and result
* `SearchRequests` - Initiation response when search request is made direct to the matching component

## Changelog
All notable data structure changes to this project will be documented in this file.

This includes both schema and data workflow changes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versions

### 1.6.0
* Added `MatchingStartTime` property to `ResultSet` model.

### 1.5.0
* Versioning and changelog added as client will now be published as NuGet package.
* Removed `SearchableDonorInformation` model and `DonorType` extensions as they are not used by the Matching Algorithm API.
