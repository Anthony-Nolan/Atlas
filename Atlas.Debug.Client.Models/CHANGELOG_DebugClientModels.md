# Atlas.Debug.Client.Models

## Description
This package contains client models utilised by the Atlas debug endpoints.

## Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### 3.1.1
* Created new namespace `Atlas.Debug.Client.Models.SearchTracking`, that contains models used in debug endpoints.

### 3.1.0
* Extended existing model, `PeekServiceBusMessagesResponse` with new prop `LastSequenceNumber`

### 3.0.0
* Updated .NET version from 6.0 to 8.0

### 2.2.0
* Created new namespace `Atlas.Debug.Client.Models.MatchPrediction` and moved various models related to debugging match prediction here.
  * Extended existing model, `GenotypeImputationResponse`, with new prop, `GenotypeCount`.
  * Changed type of prop `MatchedGenotypePairs` on existing model `GenotypeMatcherResponse` from `string` to `IEnumerable<string>`.
      * Instead of one, potentially very long, formatted string, the matched genotype pairs are now returned as a collection of formatted strings, one for each matching patient-donor genotype pair.

### 2.1.0
* Creation of new library, `Atlas.Debug.Client.Models`, that contains models used in debug endpoints.
* Moved following models to new project:
    * `DebugDonorResult` to `DonorImport` namespace.
    * `PeekServiceBusMessagesRequest` and `PeekedServiceBusMessage<T>` to `ServiceBus` namespace.
* Added new models:
    * `DonorUpdateFailureInfo` and `DonorImportRequest` to `DonorImport` namespace.